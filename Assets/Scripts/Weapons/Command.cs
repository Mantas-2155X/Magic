using AI.Interfaces;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Command : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;
			
			var alive = hit.collider.GetComponent<IAlive>();
			if (alive != null)
			{
				foreach (var npc in AIManager.Instance.NPCs)
				{
					if (!npc.IsAlive || (IAlive)npc == alive)
						continue;

					npc.FindAndKill((Component)alive);
				}
				
				return;
			}
			
			foreach (var npc in AIManager.Instance.NPCs)
			{
				if (!npc.IsAlive)
					continue;

				npc.Chill();
				npc.Walk(hit.point);
			}
		}
	}
}