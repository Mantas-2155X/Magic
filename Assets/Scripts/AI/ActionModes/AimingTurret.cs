using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class AimingTurret : IActionMode
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
			if (Owner.AIMode != EAIMode.Action || Owner.Target == null)
				return;
			
			var transform = Owner.transform;
			var target = Owner.Target.transform;
			
			if (!Owner.WithinRange.DistanceCheck(transform, target))
				return;

			if (!Owner.AimAt.AimTowardsTarget(transform, target))
				return;
			
			if (Owner.HasSight.SightCheck(Owner, target))
			{
				Owner.Weapon?.StartCasting();
				Owner.Weapon?.FinishCasting();
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