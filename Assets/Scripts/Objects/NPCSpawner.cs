using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Base;
using Objects.Enums;
using Objects.Events;
using ScriptableObjects;
using State.Interfaces;
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
		public string[] SpawnIDs;
		
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
		
		[Header("AI")]
		[SerializeField]
		public bool WanderAction;

		[SerializeField]
		public PathData PatrolAction;

		[SerializeField]
		public BaseObject UseAction;

		[SerializeField]
		public BaseObject CarryAction;
		[SerializeField]
		public Vector3 DropAtLocation;
		
		public Dictionary<string, IAlive> Spawned { get; private set; } = new ();
		
		public bool Cleared { get; private set; }
		public bool Activated { get; private set; }
		public int Triggered { get; private set; }

		private bool firstSpawn = true;
		
		#region Identify / SaveLoad
		
		public override bool ExternallySpawned { get => false; set { } }
		
		public override Dictionary<string, JObject> GetModifications()
		{
			var dict = base.GetModifications();
			dict[typeof(NPCSpawner).ToString()] = JObject.FromObject(new NPCSpawnerState(this));
			
			return dict;
		}

		public override void ApplyModifications(Dictionary<string, JObject> data)
		{
			base.ApplyModifications(data);
			
			if (data.TryGetValue(typeof(NPCSpawner).ToString(), out var npcSpawnerState) && npcSpawnerState != null)
				npcSpawnerState.ToObject<NPCSpawnerState>().Apply(this);
		}
		
		public void SetState(List<string> spawned, int triggered, bool cleared)
		{
			Cleared = cleared;
			
			// Skip ahead if needed
			for (var i = 0; i < spawned.Count; i++)
			{
				var spawnID = spawned[i];
				
				// Killed already, count as spawned
				if (StateManager.Instance.GetKilledAlives().Contains(spawnID))
				{
					Spawned.TryAdd(spawnID, null);
					continue;
				}
				
				// Spawned before this was called, ignore
				if (Spawned.ContainsKey(spawnID))
					continue;
				
				spawn(spawned[i], false);
			}

			if (Initialization == ESpawnerInitialization.OnTrigger)
			{
				for (var i = 0; i < triggered; i++)
					Trigger();
			}
		}
		
		#endregion
		
		public void Start()
		{
			if (Activated || Initialization != ESpawnerInitialization.OnStart)
				return;
			
			processLoop().Forget();
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Cleared || Spawned.Count < SpawnCount)
				return;

			foreach (var pair in Spawned)
			{
				var alive = pair.Value;
				if (alive.NotNull() && alive.IsAlive)
					return;
			}
			
			Cleared = true;
			OnSpawnerClearedEvent?.Invoke();
		}
		
		public void Trigger()
		{
			if (Activated || Initialization != ESpawnerInitialization.OnTrigger)
				return;

			Triggered++;
			
			if (Triggered < TriggerCount)
				return;
			
			processLoop().Forget();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnSpawnerClearedEvent, Color.blue);

			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0, 1));
			
			Gizmos.DrawLine(new Vector3(-0.5f, 0, -0.5f), new Vector3(0, 0, 0.5f));
			Gizmos.DrawLine(new Vector3(0.5f, 0, -0.5f), new Vector3(0, 0, 0.5f));
			
			Gizmos.DrawLine(Vector3.zero, Vector3.up);
		}

		public void OnDrawGizmosSelected()
		{
			if (Datas == null)
				return;

			for (var i = 0; i < Datas.Count; i++)
				Gizmos.DrawWireSphere(transform.position, Datas[i].SenseRange);
		}
#endif

		private async UniTaskVoid processLoop()
		{
			Activated = true;
			
			if (Datas.Count == 0)
				return;

			var waitForNext = true;
			
			while (true)
			{
				// Spawn count is reached, stop
				if (Spawned.Count >= SpawnCount)
					break;
				
				var spawnID = SpawnIDs[Spawned.Count];
				
				if (StateManager.Instance.GetKilledAlives().Contains(spawnID))
				{
					// Already died
					Spawned.TryAdd(spawnID, null);
					waitForNext = false;
					continue;
				}
				
				// Already spawned before, don't wait spawnrate
				if (Spawned.ContainsKey(spawnID))
				{
					waitForNext = false;
					continue;
				}

				if (waitForNext)
					await UniTask.WaitForSeconds(SpawnRate);
				else
					waitForNext = true;
				
				// Do it again because async is fun
				if (Spawned.ContainsKey(spawnID))
				{
					waitForNext = false;
					continue;
				}

				if (this == null || !isActiveAndEnabled)
					return;
				
				// Spawn count is reached, stop
				if (Spawned.Count >= SpawnCount)
					break;

				var currentlyAlive = 0;
				
				foreach (var pair in Spawned)
				{
					var alive = pair.Value;
					if (alive.IsNull() || !alive.IsAlive)
						continue;

					currentlyAlive++;
				}
				
				// Alive count is reached, pause
				if (currentlyAlive >= AliveCount)
					continue;
				
				var usePortal = !(Initialization == ESpawnerInitialization.OnStart && firstSpawn && SpawnRate < 0.3f);
				spawn(spawnID, usePortal);
			}
		}

		private void spawn(string spawnID, bool usePortal)
		{
			firstSpawn = false;
			
			var tr = GetTransform();

			var npc = AIManager.Instance.CreateNPC(tr.position, tr.eulerAngles, Datas[Random.Range(0, Datas.Count)], ObjectID, RelationshipGroup < -1 ? Random.Range(0, 9999) : RelationshipGroup, usePortal);
			if (npc == null || !npc.IsAlive)
			{
				Debug.LogWarning($"[NPCSpawner {gameObject.name}] Failed creating NPC");
				return;
			}

			npc.ObjectID = spawnID;

			if (WanderAction)
				npc.Wander();
			else if (PatrolAction != null)
				npc.Patrol(PatrolAction);
			else if (UseAction != null)
				npc.Use(UseAction);
			else if (CarryAction != null)
				npc.Carry(CarryAction, DropAtLocation);
			else
				npc.Idle();

			Spawned.Add(spawnID, npc);
		}
		
		[JsonObject]
		public class NPCSpawnerState : IState
		{
			[JsonProperty]
			public List<string> Spawned;
		
			[JsonProperty]
			public int Triggered;

			[JsonProperty]
			public bool Cleared;

			public NPCSpawnerState() { }
			
			public NPCSpawnerState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not NPCSpawner npcSpawner)
					return;

				Spawned = new List<string>();

				foreach (var pair in npcSpawner.Spawned)
					Spawned.Add(pair.Key);

				Triggered = npcSpawner.Triggered;
				Cleared = npcSpawner.Cleared;
			}
			
			public void Apply(object obj)
			{
				if (obj is not NPCSpawner npcSpawner)
					return;

				npcSpawner.SetState(Spawned, Triggered, Cleared);
			}
		}
	}
}