using AI.Enums;
using Managers;
using Tools;
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

				npc.SenseRange = 99999;
				npc.SetRelationshipGroup(Random.Range(1, int.MaxValue));
				npc.TakeWeapon(ObjectManager.Instance.CreateWeapon(ObjectManager.Instance.GetWeapon("Fire Gun"), Vector3.zero, Vector3.zero));
				npc.WanderAggressively(npc.Target);
			}
			
			return true;
		}
	}
}