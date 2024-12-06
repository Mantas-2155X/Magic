using AI.Enums;
using Managers;
using UnityEngine;
using Weapons.Base;
using Weapons.Interfaces;

namespace Weapons
{
	public class Deathmatch : BaseWeapon
	{
		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;

			foreach (var npc in AIManager.Instance.NPCs)
			{
				if (!npc.IsAlive)
					continue;
				
				npc.SenseRange = 9999;
					
				npc.AutoTargetRange = 9999;
				npc.AutoTarget = EAutoTarget.NPCs;
				
				npc.TakeWeapon(Instantiate(Resources.Load<GameObject>("Weapons/FireGun")).GetComponent<IWeapon>());
				npc.FindAndKill(npc.Target, false, true);
			}

			return true;
		}
	}
}