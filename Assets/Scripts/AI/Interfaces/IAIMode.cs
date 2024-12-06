using UnityEngine;

namespace AI.Interfaces
{
	public interface IAIMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner);
		
		public void Disabled();
		
		public void Update();
		
		public void TargetChanged(Component previousTarget, Component newTarget);
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination);
	}
}