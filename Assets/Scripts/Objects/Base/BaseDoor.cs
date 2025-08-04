using System;
using System.Collections.Generic;
using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Enums;
using Objects.Events;
using Objects.Interfaces;
using State.Enums;
using State.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace Objects.Base
{
	public class BaseDoor : BaseObject, IDoor
	{
		[SerializeField]
		public OnDoorOpenedEvent OnDoorOpenedEvent = new ();
		
		[SerializeField]
		public OnDoorOpeningEvent OnDoorOpeningEvent = new ();

		[SerializeField]
		public OnDoorClosedEvent OnDoorClosedEvent = new ();

		[SerializeField]
		public OnDoorClosingEvent OnDoorClosingEvent = new ();

		[field: SerializeField]
		public NavMeshObstacle Obstacle { get; private set; }
		[field: SerializeField]
		public AnimationCurve Curve { get; private set; }

		[field: FormerlySerializedAs("<State>k__BackingField")]
		[field: SerializeField]
		public EDoorState DoorState { get; private set; } = EDoorState.Closed;

		[field: SerializeField]
		public bool Interruptible { get; private set; }
		[field: SerializeField]
		public bool Locked { get; private set; }

		[field: SerializeField]
		public EDoorType Type { get; private set; }
		[field: SerializeField]
		public EDoorDirection Direction { get; private set; }

		[field: SerializeField]
		public float AutoClose { get; private set; }
		[field: SerializeField]
		public float Amount { get; private set; } = 1f;
		[field: SerializeField]
		public float Duration { get; private set; } = 0.5f;

		public float Normalized { get; private set; }
		
		private CancellationTokenSource cancellationToken = new ();

		private float lastOpened;

		#region Identify / SaveLoad
		
		public override ELoadType LoadType => ELoadType.Modify;
		
		public override ELoadTiming LoadTiming => ELoadTiming.Late;
		
		public override Dictionary<string, JObject> GetModifications()
		{
			var dict = base.GetModifications();
			dict[typeof(BaseDoor).ToString()] = JObject.FromObject(new BaseDoorState(this));
			
			return dict;
		}

		public override void ApplyModifications(Dictionary<string, JObject> data)
		{
			base.ApplyModifications(data);
			
			if (data.TryGetValue(typeof(BaseDoor).ToString(), out var baseDoorState) && baseDoorState != null)
				baseDoorState.ToObject<BaseDoorState>().Apply(this);
		}
		
		public void SetState(EDoorState state, float normalized, bool locked)
		{
			DoorState = state;
			Normalized = normalized;
			Locked = locked;

			switch (state)
			{
				case EDoorState.Open:
					lastOpened = Time.time;
					Obstacle.enabled = false;
					OnDoorOpenedEvent?.Invoke();
					break;
				case EDoorState.Closed:
					Obstacle.enabled = true;
					OnDoorClosedEvent?.Invoke();
					break;
			}
			
			setPosition();

			if (state is EDoorState.Opening or EDoorState.Closing)
			{
				DoorState = state is EDoorState.Opening ? EDoorState.Closed : EDoorState.Open;
				
				var previousLocked = Locked;
				var previousInterruptible = Interruptible;

				Locked = false;
				Interruptible = true;

				Toggle(state is EDoorState.Opening);

				Locked = previousLocked;
				Interruptible = previousInterruptible;
			}
		}
		
		#endregion
		
		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			switch (DoorState)
			{
				case EDoorState.Open:
					SetState(EDoorState.Open, 1f, Locked);
					break;
				case EDoorState.Closed:
					SetState(EDoorState.Closed, 0f, Locked);
					break;
			}
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (AutoClose == 0f || DoorState != EDoorState.Open)
				return;
			
			if (Time.time < AutoClose + lastOpened)
				return;
			
			Close();
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnDoorOpenedEvent, Color.blue);
			EventTools.DrawListeners(transform, OnDoorOpeningEvent, Color.cyan);
			EventTools.DrawListeners(transform, OnDoorClosedEvent, Color.red);
			EventTools.DrawListeners(transform, OnDoorClosingEvent, Color.yellow);
		}
#endif

		#endregion

		#region IObject

		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			Toggle();
			return true;
		}
		
		public override bool CanUse(IAlive user)
		{
			return base.CanUse(user) && !Locked;
		}

		#endregion
		
		#region Door
		
		public void Open()
		{
			Toggle(true);
		}
		public void Close()
		{
			Toggle(false);
		}

		public void Toggle()
		{
			if (Locked)
				return;

			switch (DoorState)
			{
				case EDoorState.Open or EDoorState.Opening:
					Close();
					break;
				case EDoorState.Closed or EDoorState.Closing:
					Open();
					break;
			}
		}
		public void Toggle(bool state)
		{
			if (Locked)
				return;

			if (!Interruptible && DoorState is EDoorState.Opening or EDoorState.Closing)
				return;
			
			if (state)
			{
				if (DoorState is EDoorState.Open or EDoorState.Opening)
					return;

				DoorState = EDoorState.Opening;
				Obstacle.enabled = true;
				OnDoorOpeningEvent?.Invoke();
			}
			else
			{
				if (DoorState is EDoorState.Closed or EDoorState.Closing)
					return;

				DoorState = EDoorState.Closing;
				Obstacle.enabled = true;
				OnDoorClosingEvent?.Invoke();
			}

			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();
			
			perform(cancellationToken.Token).Forget();
		}
		
		public void Lock(bool state)
		{
			Locked = state;
		}

		#endregion

		#region Internal

		private void setPosition()
		{
			var curveValue = Curve.Evaluate(Normalized);
			var doorTr = GetTransform();

			switch (Type)
			{
				case EDoorType.Sliding:
					var position = doorTr.localPosition;

					switch (Direction)
					{
						case EDoorDirection.Up:
							position.y = curveValue * Amount;
							break;
						case EDoorDirection.Down:
							position.y = -curveValue * Amount;
							break;
						case EDoorDirection.Left:
							position.x = curveValue * Amount;
							break;
						case EDoorDirection.Right:
							position.x = -curveValue * Amount;
							break;
					}

					doorTr.localPosition = position;
						
					break;
				case EDoorType.Rotating:
					throw new NotImplementedException();
			}
		}
		
		private async UniTask perform(CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;
			
			while (true)
			{
				if (token.IsCancellationRequested)
					return;

				switch (DoorState)
				{
					case EDoorState.Opening when Normalized >= 1f:
						DoorState = EDoorState.Open;
						Normalized = 1f;
						setPosition();
						Obstacle.enabled = false;
						lastOpened = Time.time;
						OnDoorOpenedEvent?.Invoke();
						return;
					case EDoorState.Closing when Normalized <= 0f:
						DoorState = EDoorState.Closed;
						Normalized = 0f;
						setPosition();
						Obstacle.enabled = true;
						OnDoorClosedEvent?.Invoke();
						return;
				}

				await UniTask.NextFrame(token);
				
				if (this == null)
					return;
				
				if (token.IsCancellationRequested)
					return;
				
				setPosition();
				
				switch (DoorState)
				{
					case EDoorState.Opening:
						Normalized += Time.deltaTime / Duration;
						break;
					case EDoorState.Closing:
						Normalized -= Time.deltaTime / Duration;
						break;
				}
			}
		}
		
		#endregion
		
		[JsonObject]
		public class BaseDoorState : IState
		{
			[JsonProperty]
			public EDoorState State;

			[JsonProperty]
			public bool Locked;

			[JsonProperty]
			public float Normalized;
			
			public BaseDoorState() { }
			
			public BaseDoorState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not BaseDoor baseDoor)
					return;

				State = baseDoor.DoorState;
				Locked = baseDoor.Locked;
				Normalized = baseDoor.Normalized;
			}
			
			public void Apply(object obj)
			{
				if (obj is not BaseDoor baseDoor)
					return;

				baseDoor.SetState(State, Normalized, Locked);
			}
		}
	}
}