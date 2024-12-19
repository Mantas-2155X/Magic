using System;
using Casts;
using Managers;
using Objects;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class HealthGun : BaseWeapon
	{
		[SerializeField]
		public float Lifetime = 15f;
		
		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			ObjectManager.Instance.CreatePool(typeof(HealthPool), LastHit.point + Vector3.up * 0.06f, Lifetime);
			return true;
		}
	}
}