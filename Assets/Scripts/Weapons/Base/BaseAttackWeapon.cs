using Managers;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseAttackWeapon : BaseWeapon, IAttackWeapon
	{
		[field: SerializeField]
		public AttackData Attack { get; private set; }

		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			ObjectManager.Instance.CreateAttack(Attack, this, LastHit, LastHit.transform);
			return true;
		}
	}
}