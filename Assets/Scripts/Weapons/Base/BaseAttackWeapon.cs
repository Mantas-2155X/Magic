using System;
using Managers;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseAttackWeapon : BaseWeapon, IAttackWeapon
	{
		public virtual Type Attack { get; private set; }

		public override void FinishCasting()
		{
			base.FinishCasting();
			
			ObjectManager.Instance.CreateAttack(Attack, Owner, LastHit.point);
		}
	}
}