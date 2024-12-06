using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class FindAndKill : IActionMode
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
			if (Owner.Target == null)
				return;

			var target = Owner.Target.transform;
			var transform = Owner.transform;

			if (!Owner.WithinRange.DistanceCheck(transform, target))
				return;

			if (!Owner.Chase.ChaseTarget(Owner, target))
				return;

			if (Owner.AIMode == EAIMode.Walking)
			{
				Owner.ReturnAIMode();
				return;
			}
			
			if (Owner.AIMode == EAIMode.Action)
			{
				if (!Owner.AimAt.AimTowardsTarget(Owner.transform, target))
					return;
			
				if (Owner.HasSight.SightCheck(Owner, target))
					Owner.Weapon?.Attack();
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