using System.Threading;
using AI.Interfaces;
using Components;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Enums;
using Objects.Events;
using Objects.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Base
{
	public class BaseElevator : BaseObject, IElevator
	{
		[SerializeField]
		public OnElevatorElevatedEvent OnElevatorElevatedEvent = new ();
		
		[SerializeField]
		public OnElevatorElevatingEvent OnElevatorElevatingEvent = new ();

		[SerializeField]
		public OnElevatorLoweredEvent OnElevatorLoweredEvent = new ();

		[SerializeField]
		public OnElevatorLoweringEvent OnElevatorLoweringEvent = new ();

		[field: SerializeField]
		public AnimationCurve Curve { get; private set; }

		[field: SerializeField]
		public Collider AntiCrush { get; private set; }

		[field: SerializeField]
		public Parent Parent { get; private set; }
		
		[field: FormerlySerializedAs("<State>k__BackingField")]
		[field: SerializeField]
		public EElevatorState ElevatorState { get; private set; } = EElevatorState.Lowered;

		[field: SerializeField]
		public bool ParentElevating { get; private set; }
		[field: SerializeField]
		public bool ParentLowering { get; private set; }
		
		[field: SerializeField]
		public bool Interruptible { get; private set; }
		[field: SerializeField]
		public bool Locked { get; private set; }

		[field: SerializeField]
		public float AutoElevate { get; private set; }
		[field: SerializeField]
		public float AutoLower { get; private set; }
		
		[field: SerializeField]
		public float Amount { get; private set; } = 1f;
		[field: SerializeField]
		public float Duration { get; private set; } = 1f;

		public float Normalized { get; private set; }
		
		private CancellationTokenSource cancellationToken = new ();
		
		private float lastElevated;
		private float lastLowered;

		#region Identify / SaveLoad

		public override bool ShouldSave => false;

		#endregion
		
		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			switch (ElevatorState)
			{
				case EElevatorState.Elevated:
					Normalized = 1f;
					Parent.Toggle(ParentLowering);
					lastElevated = Time.time;
					break;
				case EElevatorState.Lowered:
					Normalized = 0f;
					Parent.Toggle(ParentElevating);
					lastLowered = Time.time;
					break;
			}
			
			setPosition();
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			var yPos = GetTransform().localPosition.y;
			AntiCrush.enabled = ElevatorState == EElevatorState.Lowering && yPos < 3.5f && yPos > 1.5f;
			
			if (AutoElevate != 0f && ElevatorState == EElevatorState.Lowered)
			{
				if (Time.time >= AutoElevate + lastLowered)
				{
					Elevate();
				}
			}
			
			if (AutoLower != 0f && ElevatorState == EElevatorState.Elevated)
			{
				if (Time.time >= AutoLower + lastElevated)
				{
					Lower();
				}
			}
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnElevatorElevatedEvent, Color.blue);
			EventTools.DrawListeners(transform, OnElevatorElevatingEvent, Color.cyan);
			EventTools.DrawListeners(transform, OnElevatorLoweredEvent, Color.red);
			EventTools.DrawListeners(transform, OnElevatorLoweringEvent, Color.yellow);
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
		
		#region Elevator

		public void Elevate()
		{
			Toggle(true);
		}
		public void Lower()
		{
			Toggle(false);
		}

		public void Toggle()
		{
			if (Locked)
				return;

			switch (ElevatorState)
			{
				case EElevatorState.Elevated or EElevatorState.Elevating:
					Lower();
					break;
				case EElevatorState.Lowered or EElevatorState.Lowering:
					Elevate();
					break;
			}
		}
		public void Toggle(bool state)
		{
			if (Locked)
				return;

			if (!Interruptible && ElevatorState is EElevatorState.Elevating or EElevatorState.Lowering)
				return;
			
			if (state)
			{
				if (ElevatorState is EElevatorState.Elevated or EElevatorState.Elevating)
					return;

				ElevatorState = EElevatorState.Elevating;
				OnElevatorElevatingEvent?.Invoke();
			}
			else
			{
				if (ElevatorState is EElevatorState.Lowered or EElevatorState.Lowering)
					return;

				ElevatorState = EElevatorState.Lowering;
				OnElevatorLoweringEvent?.Invoke();
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
			var elevatorTr = GetTransform();

			var position = elevatorTr.localPosition;
			position.y = curveValue * Amount;

			elevatorTr.localPosition = position;
			
			Rigidbody.MovePosition(elevatorTr.position);
		}
		
		private async UniTask perform(CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;
			
			while (true)
			{
				if (token.IsCancellationRequested)
					return;

				switch (ElevatorState)
				{
					case EElevatorState.Elevating when Normalized >= 1f:
						ElevatorState = EElevatorState.Elevated;
						Normalized = 1f;
						setPosition();
						lastElevated = Time.time;
						Parent.Toggle(ParentLowering);
						OnElevatorElevatedEvent?.Invoke();
						return;
					case EElevatorState.Lowering when Normalized <= 0f:
						ElevatorState = EElevatorState.Lowered;
						Normalized = 0f;
						setPosition();
						lastLowered = Time.time;
						Parent.Toggle(ParentElevating);
						OnElevatorLoweredEvent?.Invoke();
						return;
				}

				await UniTask.NextFrame(token);
				
				if (this == null)
					return;
				
				if (token.IsCancellationRequested)
					return;
				
				setPosition();
				
				switch (ElevatorState)
				{
					case EElevatorState.Elevating:
						Normalized += Time.deltaTime / Duration;
						break;
					case EElevatorState.Lowering:
						Normalized -= Time.deltaTime / Duration;
						break;
				}
			}
		}
		
		#endregion
	}
}