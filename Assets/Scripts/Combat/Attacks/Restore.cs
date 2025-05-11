using System;
using Combat.Attacks.Base;
using Combat.Attacks.Enums;
using State.Interfaces;
using Tools;
using UnityEngine;

namespace Combat.Attacks
{
	public class Restore : BaseAttack
	{
		[field: SerializeField]
		public ERestoreType Type { get; private set; }
		
		[field: SerializeField]
		public float Amount { get; private set; }
		
		public override void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach)
		{
			base.Spawn(source, position, angles, attach);

			var alive = GetAlive();
			if (alive.IsNull())
				return;
			
			switch (Type)
			{
				case ERestoreType.Health:
					alive.RestoreHealth(Amount, this);
					break;
				case ERestoreType.Mana:
					alive.RestoreMana(Amount, this);
					break;
				case ERestoreType.Energy:
					alive.RestoreEnergy(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
		}
	}
}