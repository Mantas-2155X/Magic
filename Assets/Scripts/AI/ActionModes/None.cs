using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class None : IActionMode
	{
		public NPC Owner { get; set; }

		public bool ReturnAfterTargetGone => true;
		
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
		
		public void FixedUpdate()
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