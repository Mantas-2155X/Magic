using Objects.Enums;
using UnityEngine;
using UnityEngine.AI;

namespace Objects.Interfaces
{
	public interface IDoor
	{
		public NavMeshObstacle Obstacle { get; }
		public AnimationCurve Curve { get; }
		
		public EDoorState State { get; }

		public bool Interruptible { get; }
		public bool Locked { get; }

		public EDoorType Type { get; }
		public EDoorDirection Direction { get; }
		
		public float AutoClose { get; }
		public float Amount { get; }
		public float Duration { get; }
		
		public float Normalized { get; }
		
		public void Open();
		public void Close();
		
		public void Toggle();
		public void Toggle(bool state);
		
		public void Lock(bool state);
	}
}