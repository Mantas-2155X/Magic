using System;
using Attacks;
using Casts;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Destroyer : BaseWeapon
	{
		public override Type Cast => typeof(FireRing);
		
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;
			
			ObjectManager.Instance.CreateAttack(typeof(Incinerate), Owner, hit.point, Vector3.zero);
		}
	}
}