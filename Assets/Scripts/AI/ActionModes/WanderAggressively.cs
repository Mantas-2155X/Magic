using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class WanderAggressively : IActionMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			Owner.Chase.ResetChaseRange();
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			// No target, wander until one pops up
			if (Owner.Target == null)
			{
				if (Owner.AIMode != EAIMode.Walking)
					Owner.Wander.WalkRandomly(false);
				
				return;
			}

			var target = Owner.TargetTransform;

			// Target sensed but not within chasing range, wait and hope for the target to come closer
			if (!Owner.WithinRange.DistanceCheck(target))
				return;

			// Target within range, start chasing
			if (!Owner.Chase.ChaseTarget(target))
				return;

			// Reached target, stop walking and go into action
			if (Owner.AIMode == EAIMode.Walking)
			{
				Owner.ReturnAIMode();
				return;
			}
			
			if (Owner.AIMode == EAIMode.Action)
			{
				// Turn towards the target and aim
				if (!Owner.AimAt.AimTowardsTarget(target))
					return;
			
				Owner.Weapon?.StartCasting();
			}
		}
		
		public void TargetChanged(Component previousTarget, Component newTarget)
		{

		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}