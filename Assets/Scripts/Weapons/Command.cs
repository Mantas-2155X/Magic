using AI.Enums;
using AI.Interfaces;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Command : BaseWeapon
	{
		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;
			
			if (!Physics.Raycast(Ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return false;
			
			var rb = hit.rigidbody;
			if (rb == null)
			{
				foreach (var npc in AIManager.Instance.NPCs)
				{
					if (!npc.IsAlive)
						continue;

					npc.Chill();
					npc.Walk(hit.point);
				}

				return true;
			}

			var alive = rb.GetComponent<IAlive>();
			if (alive == null)
				return false;
			
			foreach (var npc in AIManager.Instance.NPCs)
			{
				if (!npc.IsAlive || (IAlive)npc == alive)
					continue;
				
				npc.Act((Component)alive, EActionMode.ChaseAndKill, true);
			}

			return true;
		}
	}
}