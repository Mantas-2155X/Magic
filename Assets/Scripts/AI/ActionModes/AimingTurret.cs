using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class AimingTurret : IActionMode
	{
		public NPC Owner { get; set; }

		public bool ReturnAfterTargetGone => true;
		
		private float followRange = 15f;

		private bool currentlyFollowing;

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
			var targetToOwnerDistance = Vector3.Distance(Owner.Target.transform.position, Owner.transform.position);
			currentlyFollowing = targetToOwnerDistance <= followRange;
		}
		
		public void FixedUpdate()
		{
			if (Owner.AIMode != EAIMode.Action || Owner.Target == null || !currentlyFollowing)
				return;
			
			var target = Owner.Target.transform;
			var transform = Owner.transform;
			var weapon = Owner.Weapon;

			var targetPosition = target.position - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(9f, 12f));
			
			if (Quaternion.Angle(transform.rotation, targetRotation) < 5f && Owner.HasSightOf(Owner.Target, 11f))
				weapon?.Attack();
		}
		
		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}