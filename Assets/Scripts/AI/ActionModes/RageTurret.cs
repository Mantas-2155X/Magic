using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class RageTurret : IActionMode
	{
		public NPC Owner { get; set; }
		
		public bool ReturnAfterTargetGone => false;

		private readonly Vector2 degreesRange = new (9f, 12f);
		
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
			if (Owner.AIMode != EAIMode.Action)
				return;
			
			var transform = Owner.transform;
			transform.Rotate(transform.up, Random.Range(degreesRange.x, degreesRange.y));
			
			Owner.Weapon?.Attack();
		}

		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}