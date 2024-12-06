using AI.Interfaces;
using UnityEngine;

namespace AI.AIModes
{
	public class Idle : IAIMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			
		}

		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}