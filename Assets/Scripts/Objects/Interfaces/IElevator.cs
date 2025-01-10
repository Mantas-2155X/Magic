using Objects.Enums;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IElevator
	{
		public AnimationCurve Curve { get; }
		
		public EElevatorState State { get; }

		public bool Interruptible { get; }
		public bool Locked { get; }
		
		public float Amount { get; }
		public float Duration { get; }
		
		public float Normalized { get; }
		
		public void Elevate();
		public void Lower();
		
		public void Toggle();
		public void Toggle(bool state);
		
		public void Lock(bool state);
	}
}