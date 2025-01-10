using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Enums;
using Objects.Events;
using Objects.Interfaces;
using UnityEngine;

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
		public EElevatorState State { get; private set; } = EElevatorState.Lowered;

		[field: SerializeField]
		public bool Interruptible { get; private set; }
		[field: SerializeField]
		public bool Locked { get; private set; }

		[field: SerializeField]
		public float Amount { get; private set; } = 1f;
		[field: SerializeField]
		public float Duration { get; private set; } = 1f;

		public float Normalized { get; private set; }
		
		private CancellationTokenSource cancellationToken = new ();
		
		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			switch (State)
			{
				case EElevatorState.Elevated:
					Normalized = 1f;
					break;
				case EElevatorState.Lowered:
					Normalized = 0f;
					break;
			}
			
			setPosition();
		}

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

			switch (State)
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

			if (!Interruptible && State is EElevatorState.Elevating or EElevatorState.Lowering)
				return;
			
			if (state)
			{
				if (State is EElevatorState.Elevated or EElevatorState.Elevating)
					return;

				State = EElevatorState.Elevating;
				OnElevatorElevatingEvent?.Invoke();
			}
			else
			{
				if (State is EElevatorState.Lowered or EElevatorState.Lowering)
					return;

				State = EElevatorState.Lowering;
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
					case EElevatorState.Elevating when Normalized >= 1f:
						State = EElevatorState.Elevated;
						Normalized = 1f;
						setPosition();
						OnElevatorElevatedEvent?.Invoke();
						return;
					case EElevatorState.Lowering when Normalized <= 0f:
						State = EElevatorState.Lowered;
						Normalized = 0f;
						setPosition();
						OnElevatorLoweredEvent?.Invoke();
						return;
				}

				await UniTask.NextFrame(token);
				
				if (token.IsCancellationRequested)
					return;
				
				setPosition();
				
				switch (State)
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