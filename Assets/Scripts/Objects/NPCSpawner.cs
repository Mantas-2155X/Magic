using Managers;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	// todo: make this configurable
	public class NPCSpawner : BaseObject
	{
		public void Start()
		{
			var tr = GetTransform();
			
			var npc = AIManager.Instance.CreateNPC(tr.position, tr.eulerAngles);
			npc.LearnSpell(ObjectManager.Instance.GetSpell("Fire Ball"), true);
			npc.WaitAggressively();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0, 1));
			
			Gizmos.DrawLine(new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0));
			Gizmos.DrawLine(new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0));
		}
#endif
	}
}