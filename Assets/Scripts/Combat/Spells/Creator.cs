using Combat.Spells.Base;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Spells
{
	public class Creator : BaseSpell
	{
		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			AIManager.Instance.CreateNPC(LastHit.point + Vector3.up * 1.25f, new Vector3(0, Owner.GetTransform().eulerAngles.y, 0), (NPCData)ObjectManager.Instance.GetAlive("NPC"));
			return true;
		}
	}
}