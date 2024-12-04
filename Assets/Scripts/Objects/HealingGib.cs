using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class HealingGib : BaseUsable
	{
		public override bool DestroyAfterUse => true;

		[SerializeField]
		public int HealAmount = 10;
		
		public override void Use(IAlive user)
		{
			user.Heal(HealAmount, this);
			
			base.Use(user);
		}
	}
}