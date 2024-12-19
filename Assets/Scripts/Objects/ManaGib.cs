using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class ManaGib : BaseObject
	{
		[SerializeField]
		public float GenerateAmount;
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			user.GenerateMana(GenerateAmount, this);
			return true;
		}
	}
}