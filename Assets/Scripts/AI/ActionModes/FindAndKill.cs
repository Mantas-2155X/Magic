using AI.ActionModes.Shared;
using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class FindAndKill : IActionMode
	{
		public NPC Owner { get; set; }
		
		private bool shouldReach;
		
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
			if (Owner.Target == null)
				return;

			var target = Owner.Target.transform;
			var transform = Owner.transform;

			shouldReach = Owner.WithinRange.DistanceCheck(transform, target);
			if (!shouldReach)
				return;

			var reachedTarget = Owner.Chase.ChaseTarget(Owner, target);
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
			
			if (!Owner.AimAt.AimTowardsTarget(Owner.transform, target))
				return;
			
			if (Owner.HasSight.SightCheck(Owner, target))
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