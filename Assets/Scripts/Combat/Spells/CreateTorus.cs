using System;
using AI;
using AI.Base;
using AI.Interfaces;
using Combat.Spells.Base;
using Managers;
using ScriptableObjects;
using Tools;
using UnityEngine;

namespace Combat.Spells
{
	public class CreateTorus : BaseSpell
	{
		public override bool FinishCasting()
		{
			if (!base.FinishCasting())
				return false;

			var core = Owner.Body.Core;
			var spawnPos = core.position + core.up;
			
			var torus = AIManager.Instance.CreateNPC(spawnPos, Vector3.zero, (NPCData)ObjectManager.Instance.GetAlive("AI_TORUS_NAME"), Owner.RelationshipGroup);

			BaseAlive target = null;
			
			switch (Owner)
			{
				case Player:
				{
					var rb = LastHit.rigidbody;
					if (rb != null)
						target = rb.GetComponent<BaseAlive>();
					break;
				}
				case NPC npc:
				{
					if (npc.AttackTarget.NotNull() && npc.AttackTarget is BaseAlive alive)
						target = alive;
					break;
				}
				default:
					throw new NotImplementedException();
			}

			torus.Wander();
			
			if (target != null)
				torus.AssignAttackTarget(target);
			
			return true;
		}
	}
}