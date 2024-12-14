using System;
using Casts;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Shield : BaseWeapon
	{
		public override Type Cast => typeof(ManaRing);
		
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;
			
			ObjectManager.Instance.CreateAttack(typeof(Attacks.Shield), Owner, hit.point, Owner.GetGameObject().transform.eulerAngles);
		}
	}
}