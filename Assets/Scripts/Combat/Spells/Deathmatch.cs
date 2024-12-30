using Combat.Spells.Base;
using Managers;
using UnityEngine;

namespace Combat.Spells
{
	public class Deathmatch : BaseSpell
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
				npc.GrantSpell(ObjectManager.Instance.GetSpell("Fire Ball"), true);
				npc.WanderAggressively();
			}
			
			return true;
		}
	}
}