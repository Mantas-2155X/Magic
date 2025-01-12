using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Base;
using Objects.Enums;
using Objects.Events;
using ScriptableObjects;
using Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Objects
{
	public class NPCSpawner : BaseObject
	{
		[SerializeField]
		public List<NPCData> Datas;

		[SerializeField]
		public ESpawnerInitialization Initialization;
		
		[SerializeField]
		public int RelationshipGroup;
		
		[SerializeField]
		public int SpawnCount = 1;
		
		[SerializeField]
		public int AliveCount = 1;
		
		[SerializeField]
		public float SpawnRate = 0.1f;

		[SerializeField]
		public OnSpawnerClearedEvent OnSpawnerClearedEvent;

		[SerializeField]
		public int TriggerCount = 1;
		
		private readonly List<IAlive> spawned = new ();
		
		private bool cleared;
		private bool activated;
		private int triggered;
		
		public void Start()
		{
			if (activated || Initialization != ESpawnerInitialization.OnStart)
				return;
			
			spawn().Forget();
		}

		public void Update()
		{
			if (cleared || spawned.Count < SpawnCount)
				return;

			for (var i = 0; i < spawned.Count; i++)
			{
				var alive = spawned[i];
				if (alive != null && alive.IsAlive)
					return;
			}
			
			cleared = true;
			OnSpawnerClearedEvent?.Invoke();
		}

		public void Trigger()
		{
			if (activated || Initialization != ESpawnerInitialization.OnTrigger)
				return;

			triggered++;
			
			if (triggered < TriggerCount)
				return;
			
			spawn().Forget();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnSpawnerClearedEvent, Color.blue);

			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0, 1));
			
			Gizmos.DrawLine(new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0));
			Gizmos.DrawLine(new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, 0));
		}
#endif

		private async UniTaskVoid spawn()
		{
			activated = true;
			
			if (Datas.Count == 0)
				return;

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
				
				npc.Idle();
				spawned.Add(npc);
			}
		}
	}
}