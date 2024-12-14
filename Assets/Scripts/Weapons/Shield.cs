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
			ObjectManager.Instance.CreateAttack(typeof(Attacks.Shield), Owner, LastHit.point, Owner.GetGameObject().transform.eulerAngles);
		}
	}
}