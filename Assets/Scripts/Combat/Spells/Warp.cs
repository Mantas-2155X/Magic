using System;
using AI;
using Combat.Spells.Base;
using Managers;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Combat.Spells
{
	public class Warp : BaseSpell
	{
		[SerializeField]
		public float WarpRange = 6f;

		public override bool FinishCasting()
		{
			OverrideRange = WarpRange;
			var status = base.FinishCasting();
			OverrideRange = -1f;
			
			if (!status)
				return false;
			
			var startPos = Owner.GetTransform().position;
			var endPos = startPos;
			
			switch (Owner)
			{
				case Player player:
				{
					endPos = LastHit.point;
					
					if (LastHit.transform != null)
						endPos += LastHit.normal;
					
					player.GetTransform().position = endPos;
					player.Body.Rigidbody.MovePosition(endPos);

					break;
				}
				case NPC npc:
				{
					var tryCircle = true;

					if (npc.AttackTarget != null)
					{
						endPos = npc.AttackTargetTransform.position - npc.AttackTargetTransform.forward * Random.Range(1.5f, WarpRange);

						// If the destination is unreachable, try at the circle around the npc instead
						if (!NavMesh.Raycast(startPos, endPos, out _, NavMesh.AllAreas))
							tryCircle = false;
					}

					if (tryCircle)
					{
						var circle = Random.insideUnitCircle * WarpRange;
						endPos = new Vector3(startPos.x + circle.x, startPos.y, startPos.z + circle.y);

						// Prevent picking a destination that's behind a wall
						if (NavMesh.Raycast(startPos, endPos, out _, NavMesh.AllAreas))
							return false;
					}

					var tr = npc.GetTransform();
					endPos += tr.up * ((npc.Agent.baseOffset * tr.localScale.y) / 2f);
					
					tr.position = endPos;
					npc.Body.Rigidbody.MovePosition(endPos);
					
					break;
				}
				default:
					throw new NotImplementedException();
			}

			var portal = ObjectManager.Instance.GetObject("Portal");
			
			ObjectManager.Instance.CreateObject(portal, startPos, Vector3.zero);
			ObjectManager.Instance.CreateObject(portal, endPos, Vector3.zero);
			
			return true;
		}
	}
}