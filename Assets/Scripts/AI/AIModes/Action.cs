using AI.Interfaces;
using UnityEngine;

namespace AI.AIModes
{
	public class Action : IAIMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			
			if (Owner.Target == null)
				endActionIfNeeded();
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
			if (newTarget == null)
				endActionIfNeeded();
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}

		private void endActionIfNeeded()
		{
			if (Owner.ActWithoutTarget)
				return;
				
			Owner.ReturnAIMode(true);
		}
	}
}