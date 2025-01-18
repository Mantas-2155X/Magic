using Objects.Enums;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IConveyor
	{
		public AnimationCurve Curve { get; }

		public Rigidbody Rigidbody { get; }
		
		public EConveyorState State { get; }
		
		public bool Interruptible { get; }
		public bool Locked { get; }

		public float Speed { get; }
		public float StartStopDuration { get; }
		
		public float Normalized { get; }

		public void Run();
		public void Stop();
		
		public void Toggle();
		public void Toggle(bool state);
		
		public void Lock(bool state);
	}
}