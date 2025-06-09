using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Enums;
using Objects.Events;
using Objects.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Base
{
	public class BaseConveyor : BaseObject, IConveyor
	{
		[SerializeField]
		public OnConveyorRunningEvent OnConveyorRunningEvent = new ();
		
		[SerializeField]
		public OnConveyorAcceleratingEvent OnConveyorAcceleratingEvent = new ();

		[SerializeField]
		public OnConveyorStoppedEvent OnConveyorStoppedEvent = new ();

		[SerializeField]
		public OnConveyorDeceleratingEvent OnConveyorDeceleratingEvent = new ();
		
		[field: SerializeField]
		public AnimationCurve Curve { get; private set; }

		[field: FormerlySerializedAs("<State>k__BackingField")]
		[field: SerializeField]
		public EConveyorState ConveyorState { get; private set; } = EConveyorState.Stopped;
		
		[field: SerializeField]
		public bool Interruptible { get; private set; }
		[field: SerializeField]
		public bool Locked { get; private set; }

		[field: SerializeField]
		public float Speed { get; private set; } = 1f;
		[field: SerializeField]
		public float StartStopDuration { get; private set; } = 1f;
		
		public float Normalized { get; private set; }

		private CancellationTokenSource cancellationToken = new ();

		#region Identify / SaveLoad

		public override bool ShouldSave => false;

		#endregion
		
		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			
			switch (ConveyorState)
			{
				case EConveyorState.Running:
					Normalized = 1f;
					break;
				case EConveyorState.Stopped:
					Normalized = 0f;
					break;
			}
		}

		public void FixedUpdate()
		{
			if (ConveyorState == EConveyorState.Stopped)
				return;

			var currentSpeed = Speed;
			
			switch (ConveyorState)
			{
				case EConveyorState.Accelerating:
					currentSpeed *= Normalized;
					break;
				case EConveyorState.Decelerating:
					currentSpeed *= 1 - Normalized;
					break;
			}

			var forward = GetTransform().forward;
			
			Rigidbody.position -= forward * (currentSpeed * Time.deltaTime);
			Rigidbody.MovePosition(Rigidbody.position + forward * (currentSpeed * Time.deltaTime));
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

		public override bool CanUse(IAlive user)
		{
			return base.CanUse(user) && !Locked;
		}

		#endregion

		#region Conveyor

		public void Run()
		{
			Toggle(true);
		}
		public void Stop()
		{
			Toggle(false);
		}

		public void Toggle()
		{
			if (Locked)
				return;

			switch (ConveyorState)
			{
				case EConveyorState.Accelerating or EConveyorState.Running:
					Stop();
					break;
				case EConveyorState.Decelerating or EConveyorState.Stopped:
					Run();
					break;
			}
		}
		public void Toggle(bool state)
		{
			if (Locked)
				return;

			if (!Interruptible && ConveyorState is EConveyorState.Accelerating or EConveyorState.Decelerating)
				return;
			
			if (state)
			{
				if (ConveyorState is EConveyorState.Running or EConveyorState.Accelerating)
					return;

				ConveyorState = EConveyorState.Accelerating;
				OnConveyorAcceleratingEvent?.Invoke();
			}
			else
			{
				if (ConveyorState is EConveyorState.Stopped or EConveyorState.Decelerating)
					return;

				ConveyorState = EConveyorState.Decelerating;
				OnConveyorDeceleratingEvent?.Invoke();
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

		private async UniTask perform(CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;
			
			while (true)
			{
				if (token.IsCancellationRequested)
					return;

				switch (ConveyorState)
				{
					case EConveyorState.Accelerating when Normalized >= 1f:
						ConveyorState = EConveyorState.Running;
						Normalized = 1f;
						OnConveyorRunningEvent?.Invoke();
						return;
					case EConveyorState.Decelerating when Normalized <= 0f:
						ConveyorState = EConveyorState.Stopped;
						Normalized = 0f;
						OnConveyorStoppedEvent?.Invoke();
						return;
				}

				await UniTask.NextFrame(token);
				
				if (this == null)
					return;
				
				if (token.IsCancellationRequested)
					return;
				
				switch (ConveyorState)
				{
					case EConveyorState.Accelerating:
						Normalized += Time.deltaTime / StartStopDuration;
						break;
					case EConveyorState.Decelerating:
						Normalized -= Time.deltaTime / StartStopDuration;
						break;
				}
			}
		}

		#endregion
	}
}