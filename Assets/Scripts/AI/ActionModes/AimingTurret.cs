using AI.ActionModes.Shared;
using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class AimingTurret : IActionMode
	{
		public NPC Owner { get; set; }
		
		private readonly AimAt aimAt = new (9f, 12f, 5f);
		private readonly HasSight hasSight = new (11f);
		private readonly WithinRange withinRange = new (15f);
		
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
			if (Owner.AIMode != EAIMode.Action || Owner.Target == null)
				return;
			
			var transform = Owner.transform;
			var target = Owner.Target.transform;
			
			if (!withinRange.DistanceCheck(transform, target))
				return;

			if (!aimAt.AimTowardsTarget(transform, target))
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