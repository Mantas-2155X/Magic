using AI.Interfaces;
using Objects.Base;
using Objects.Enums;
using UnityEngine;

namespace Objects
{
	public class HealthGib : BaseUsable
	{
		public override float UsableAfter => 0.1f;
		public override EDestroyType DestroyAfterUse => EDestroyType.GameObject;

		[SerializeField]
		public float HealAmount;

		public override void Awake()
		{
			base.Awake();
			DisplayName = GetType().Name;
		}
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			user.Heal(HealAmount, this);
			return true;
		}
	}
}