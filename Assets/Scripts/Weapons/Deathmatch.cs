using AI.Enums;
using Managers;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Deathmatch : BaseWeapon
	{
		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;

			foreach (var npc in AIManager.Instance.NPCs)
			{
				if (!npc.IsAlive)
					continue;
				
				npc.TakeWeapon(ObjectManager.Instance.CreateWeapon(typeof(FireGun), Vector3.zero, Vector3.zero));
				npc.WanderAggressively(npc.Target);
			}
			
			return true;
		}
	}
}