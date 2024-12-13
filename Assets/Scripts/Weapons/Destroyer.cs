using Attacks;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Destroyer : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;
			
			ObjectManager.Instance.CreateAttack(typeof(Fire), Owner, hit.point);
		}
	}
}