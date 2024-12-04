using AI.Interfaces;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Destroyer : BaseWeapon
	{
		public override float TimeBetweenAttacks => 0.25f;

		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;
			
			if (!Physics.Raycast(Ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return false;

			var rb = hit.rigidbody;
			if (rb == null)
				return false;

			var alive = rb.GetComponent<IAlive>();
			alive?.Kill(this);

			return true;
		}
	}
}