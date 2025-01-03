using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Base;
using ScriptableObjects;
using UnityEngine;

namespace Objects
{
	public class NPCSpawner : BaseObject
	{
		[SerializeField]
		public List<NPCData> Datas;

		[SerializeField]
		public int RelationshipGroup;
		
		[SerializeField]
		public int SpawnCount = 1;
		
		[SerializeField]
		public int AliveCount = 1;
		
		[SerializeField]
		public float SpawnRate = 0.1f;

		private readonly List<IAlive> spawned = new ();
		
		public void Start()
		{
			if (Datas.Count == 0)
				return;
			
			spawn().Forget();
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

		private async UniTaskVoid spawn()
		{
			var tr = GetTransform();
			
			while (isActiveAndEnabled)
			{
				await UniTask.WaitForSeconds(SpawnRate);
				
				// Spawn count is reached, stop
				if (spawned.Count >= SpawnCount)
					break;

				var currentlyAlive = 0;
				
				for (var i = 0; i < spawned.Count; i++)
				{
					var alive = spawned[i];
					if (alive == null || !alive.IsAlive)
						continue;

					currentlyAlive++;
				}
				
				// Alive count is reached, pause
				if (currentlyAlive >= AliveCount)
					continue;
				
				var npc = AIManager.Instance.CreateNPC(tr.position, tr.eulerAngles, Datas[Random.Range(0, Datas.Count)], RelationshipGroup);
				if (npc == null || !npc.IsAlive)
				{
					Debug.LogWarning($"[{name}] Failed creating NPC");
					continue;
				}
				
				npc.WaitAggressively();
				spawned.Add(npc);
			}
		}
	}
}