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
		public virtual bool Attach { get; private set; }
		public virtual Type Attack { get; private set; }

		public override void FinishCasting()
		{
			base.FinishCasting();
			
			if (Attach)
			{
				Transform attach = null;
				
				var coll = LastHit.collider;
				if (coll != null)
				{
					if (coll.GetComponent<IAlive>() != null)
						attach = coll.transform;
					else if (coll.GetComponent<IBreakable>() != null)
						attach = coll.transform;
				}
				
				if (attach != null)
					ObjectManager.Instance.CreateAttack(Attack, this, attach);
				else
					ObjectManager.Instance.CreateAttack(Attack, this, LastHit.point, Quaternion.identity);
			}
			else
			{
				ObjectManager.Instance.CreateAttack(Attack, this, LastHit.point, Quaternion.identity);
			}
		}
	}
}