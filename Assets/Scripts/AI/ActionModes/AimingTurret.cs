using AI.ActionModes.Shared;
using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class AimingTurret : IActionMode
	{
		public NPC Owner { get; set; }

		public bool ReturnAfterTargetGone => true;

		private readonly AimAt aimAt = new (9f, 12f);
		private readonly WithinRange withinRange = new (15f);
		private readonly HasSight hasSight = new (5f, 11f);
		
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

			var lookRotation = aimAt.AimStep(transform, target);
			
			if (hasSight.SightCheck(Owner, target, lookRotation))
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