using System;
using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Enums;
using Objects.Events;
using Objects.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AI;

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

		[field: SerializeField]
		public EDoorState State { get; private set; } = EDoorState.Closed;

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

		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			switch (State)
			{
				case EDoorState.Open:
					Normalized = 1f;
					Obstacle.enabled = false;
					lastOpened = Time.time;
					break;
				case EDoorState.Closed:
					Normalized = 0f;
					Obstacle.enabled = true;
					break;
			}
			
			setPosition();
		}

		public void Update()
		{
			if (AutoClose == 0f || State != EDoorState.Open)
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

			switch (State)
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

			if (!Interruptible && State is EDoorState.Opening or EDoorState.Closing)
				return;
			
			if (state)
			{
				if (State is EDoorState.Open or EDoorState.Opening)
					return;

				State = EDoorState.Opening;
				Obstacle.enabled = true;
				OnDoorOpeningEvent?.Invoke();
			}
			else
			{
				if (State is EDoorState.Closed or EDoorState.Closing)
					return;

				State = EDoorState.Closing;
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

				switch (State)
				{
					case EDoorState.Opening when Normalized >= 1f:
						State = EDoorState.Open;
						Normalized = 1f;
						setPosition();
						Obstacle.enabled = false;
						lastOpened = Time.time;
						OnDoorOpenedEvent?.Invoke();
						return;
					case EDoorState.Closing when Normalized <= 0f:
						State = EDoorState.Closed;
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
				
				switch (State)
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
	}
}