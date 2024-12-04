using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Creator : BaseWeapon
	{
		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;

			if (!Physics.Raycast(Ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return false;

			AIManager.Instance.CreateNPC(hit.point + Vector3.up * 1.25f, Vector3.zero);
			return true;
		}
	}
}