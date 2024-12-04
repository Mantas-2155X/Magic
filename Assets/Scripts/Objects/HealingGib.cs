using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class HealingGib : BaseUsable
	{
		[SerializeField]
		public int HealAmount = 10;
		
		public override bool DestroyAfterUse => true;

		public override void Use(IAlive user)
		{
			user.Heal(HealAmount, this);
			
			base.Use(user);
		}
	}
}