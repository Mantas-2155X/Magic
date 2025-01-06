using System;
using Combat.Attacks.Base;
using Combat.Attacks.Enums;
using UnityEngine;

namespace Combat.Attacks
{
	public class Restore : BaseAttack
	{
		[field: SerializeField]
		public ERestoreType Type { get; private set; }
		
		[field: SerializeField]
		public float Amount { get; private set; }
		
		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);

			var alive = GetAlive();
			if (alive == null)
				return;
			
			switch (Type)
			{
				case ERestoreType.Health:
					alive.RestoreHealth(Amount, this);
					break;
				case ERestoreType.Mana:
					alive.RestoreMana(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
		}
	}
}