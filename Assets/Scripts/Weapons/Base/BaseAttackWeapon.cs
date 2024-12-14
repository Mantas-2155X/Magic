using System;
using AI.Interfaces;
using Managers;
using Objects.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseAttackWeapon : BaseWeapon, IAttackWeapon
	{
		[field: SerializeField]
		public virtual float Distance { get; private set; }
		public virtual Type Attack { get; private set; }

		public override void FinishCasting()
		{
			base.FinishCasting();
			
			if (LastHit.distance > Distance)
				return;
			
			ObjectManager.Instance.CreateAttack(Attack, this, LastHit.point, Quaternion.identity, LastHit.transform);
		}
	}
}