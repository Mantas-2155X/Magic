using System;
using AI.Interfaces;
using Objects.Enums;
using UnityEngine;

namespace Objects.Base
{
	public class BaseGib : BaseObject
	{
		[SerializeField]
		public EGibType Type;
		
		[SerializeField]
		public float Amount;
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			switch (Type)
			{
				case EGibType.Health:
					user.RestoreHealth(Amount, this);
					break;
				case EGibType.Mana:
					user.RestoreMana(Amount, this);
					break;
				case EGibType.Energy:
					user.RestoreEnergy(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
			
			return true;
		}
	}
}