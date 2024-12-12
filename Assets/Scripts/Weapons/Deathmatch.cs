using AI.Enums;
using Managers;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Deathmatch : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();

			foreach (var npc in AIManager.Instance.NPCs)
			{
				if (!npc.IsAlive)
					continue;
				
				npc.SenseRange = 9999;
					
				npc.AutoTargetRange = 9999;
				npc.AutoTarget = EAutoTarget.NPCs;
				
				npc.TakeWeapon(ObjectManager.Instance.CreateWeapon(typeof(FireGun), Vector3.zero, Vector3.zero));
				npc.FindAndKill(npc.Target, false, true);
			}
		}
	}
}