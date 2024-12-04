using UnityEngine;

namespace AI.Interfaces
{
	public interface IActionMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner);
		
		public void Disabled();
		
		public void Update();
		
		public void FixedUpdate();

		public void TargetChanged(Component previousTarget, Component newTarget);
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination);
	}
}