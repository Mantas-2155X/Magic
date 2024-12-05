using AI.ActionModes.Shared;
using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class FindAndKill : IActionMode
	{
		public NPC Owner { get; set; }
		
		private readonly AimAt aimAt = new (9f, 12f, 5f);
		private readonly WithinRange withinRange = new (25f);
		private readonly HasSight hasSight = new (11f);
		private readonly Chase chase = new (10f);
		
		private bool shouldReach;
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			chase.ResetCurrentStopAt();
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			if (Owner.Target == null)
				return;

			var target = Owner.Target.transform;
			var transform = Owner.transform;

			shouldReach = withinRange.DistanceCheck(transform, target);
			if (!shouldReach)
				return;

			var reachedTarget = chase.ChaseTarget(Owner, target);
			if (!reachedTarget)
				return;

			if (Owner.AIMode == EAIMode.Walking)
				Owner.ReturnAIMode();
		}
		
		public void FixedUpdate()
		{
			if (Owner.AIMode != EAIMode.Action || Owner.Target == null || !shouldReach)
				return;
			
			var target = Owner.Target.transform;
			
			if (!aimAt.RotateTowardsTarget(Owner.transform, target))
				return;
			
			if (hasSight.SightCheck(Owner, target))
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