using System;
using Attacks.Enums;
using Managers;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseAttackWeapon : BaseWeapon, IAttackWeapon
	{
		public virtual EAttackAngle AttackAngle { get; private set; }
		public virtual Type Attack { get; private set; }

		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			Quaternion angles;

			switch (AttackAngle)
			{
				case EAttackAngle.Identity:
					angles = Quaternion.identity;
					break;
				case EAttackAngle.HitNormal:
					angles = Quaternion.FromToRotation(Vector3.up, LastHit.normal);
					break;
				case EAttackAngle.Owner:
					angles = Owner.GetGameObject().transform.rotation;
					break;
				default:
					throw new NotImplementedException();
			}
			
			ObjectManager.Instance.CreateAttack(Attack, this, LastHit.point, angles, LastHit.transform);
			return true;
		}
	}
}