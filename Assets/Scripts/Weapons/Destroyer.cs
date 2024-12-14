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
			ObjectManager.Instance.CreateAttack(typeof(Incinerate), Owner, LastHit.point, Vector3.zero);
		}
	}
}