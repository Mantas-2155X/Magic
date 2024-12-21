using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Interfaces;
using Attacks.Interfaces;
using Projectiles.Interfaces;
using Tools;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Weapons.Interfaces;

namespace Managers
{
	public class AIManager : MonoBehaviour
	{
		public static AIManager Instance;
		
		[SerializeField]
		public Player Player;

		[SerializeField]
		public List<NPC> NPCs = new ();
		
		// Body Collider -> Alive
		public readonly Dictionary<Collider, IAlive> AlivesColliderMap = new ();
		
		// Body Collider Instance ID -> Alive
		public readonly Dictionary<int, IAlive> AlivesColliderIDMap = new ();
		
		private static bool nativeDataDirty = true;

		private readonly int overlapMaximumHits = 10;
		private readonly float autoTargetRate = 0.1f;

		private readonly List<IAlive> overlapAlives = new ();
		
		private NativeArray<OverlapSphereCommand> overlapCommandsNative;
		private NativeArray<ColliderHit> overlapHitsNative;
		
		private JobHandle overlapHandle;

		private float lastAutoTarget;

		private bool updateTargets;
		
		public void Awake()
		{
			Instance = this;
			
			BaseAlive.OnDamageEvent.AddListener(onDamage);
			BaseAlive.OnSpawnEvent.AddListener(onSpawn);
			BaseAlive.OnDeathEvent.AddListener(onDeath);
			BaseAlive.OnRelationshipGroupChangedEvent.AddListener(onRelationshipGroupChanged);
		}

		public void Update()
		{
			handleTargets();

			if (nativeDataDirty)
			{
				destroyNativeData();
				
				overlapAlives.Clear();
				
				overlapAlives.Add(Player);
				
				for (var i = 0; i < NPCs.Count; i++)
				{
					var npc = NPCs[i];
					if (!npc.IsAlive)
						continue;

					overlapAlives.Add(npc);
				}

				overlapCommandsNative = new NativeArray<OverlapSphereCommand>(overlapAlives.Count, Allocator.Persistent);
				overlapHitsNative = new NativeArray<ColliderHit>(overlapCommandsNative.Length * overlapMaximumHits, Allocator.Persistent);

				for (var i = 0; i < overlapAlives.Count; i++)
				{
					var alive = overlapAlives[i];

					var pos = alive.GetTransform().position;
					var senseRange = alive is NPC npc ? npc.SenseRange : 1f;

					overlapCommandsNative[i] = new OverlapSphereCommand(pos, senseRange, new QueryParameters
					{
						layerMask = LayerMaskTools.GetMaskAlives(),
						hitTriggers = QueryTriggerInteraction.Ignore
					});
				}
				
				nativeDataDirty = false;
			}

			var time = Time.time;
			if (lastAutoTarget + autoTargetRate > time)
				return;

			lastAutoTarget = time;
			
			overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommandsNative, overlapHitsNative, 1, overlapMaximumHits);
			overlapHandle.Complete();

			for (var i = 0; i < overlapCommandsNative.Length; i++)
			{
				var source = overlapAlives[i];
				if (source is not NPC npc || !source.IsAlive)
					continue;

				var sourcePos = npc.GetTransform().position;
				
				IAlive smallestAlive = null;
				var smallestDistance = float.MaxValue;
				
				for (var k = 0; k < overlapMaximumHits; k++)
				{
					var hit = overlapHitsNative[k + i * overlapMaximumHits];
					
					var colliderId = hit.instanceID;
					if (colliderId == 0)
						break;
					
					if (!AlivesColliderIDMap.TryGetValue(colliderId, out var target) || target == source)
						continue;
					
					if (!target.IsAlive || source.RelationshipGroup == target.RelationshipGroup)
						continue;

					var targetPos = target.GetTransform().position;
					
					var distance = math.distancesq(sourcePos, targetPos);
					if (distance >= smallestDistance)
						continue;
					
					smallestDistance = distance;
					smallestAlive = target;
				}

				if (smallestAlive == null)
					continue;

				// don't interrupt casting 
				if (npc.Weapon != null && npc.Weapon.IsCasting)
					continue;

				npc.AssignTarget((Component)smallestAlive);
			}
		}

		public void OnDestroy()
		{
			overlapHandle.Complete();
			destroyNativeData(); 
		}

		private void destroyNativeData()
		{
			if (overlapCommandsNative.IsCreated)
				overlapCommandsNative.Dispose();
			
			if (overlapHitsNative.IsCreated)
				overlapHitsNative.Dispose();
		}

		private void handleTargets()
		{
			if (!updateTargets)
				return;

			updateTargets = false;
			
			for (var i = 0; i < NPCs.Count; i++)
			{
				var npc = NPCs[i];
				if (!npc.IsAlive)
					continue;

				if (npc.Target is not IAlive target)
					continue;

				if (!target.IsAlive)
				{
					npc.AssignTarget(null);
					nativeDataDirty = true;
				}
				else if (npc.RelationshipGroup == target.RelationshipGroup)
				{
					npc.AssignTarget(null);
					nativeDataDirty = true;
				}
			}
		}

		private void onDamage(IAlive alive, float damage, object source)
		{
			if (!alive.IsAlive || alive is not NPC npc)
				return;

			IAlive aggressor = null;
			
			switch (source)
			{
				case IAlive aggr:
					aggressor = aggr;
					break;
				case IWeapon weapon:
					aggressor = weapon.GetAlive();
					break;
				case IAttack attack:
					aggressor = attack.GetAlive();
					break;
				case IProjectile projectile:
					aggressor = projectile.GetAlive();
					break;
			}
			
			if (aggressor == null || !aggressor.IsAlive || aggressor == alive || aggressor.RelationshipGroup == alive.RelationshipGroup)
				return;
			
			npc.AssignTarget((Component)aggressor);
		}

		private void onSpawn(IAlive alive)
		{
			updateTargets = true;
			nativeDataDirty = true;
		}
		
		private void onDeath(IAlive alive, object source)
		{
			var coll = alive.Body.BodyCollider;
			
			AlivesColliderMap.Remove(coll);
			AlivesColliderIDMap.Remove(coll.GetInstanceID());
			
			updateTargets = true;
			nativeDataDirty = true;
		}

		private void onRelationshipGroupChanged(IAlive alive, int previousRelationshipGroup, int newRelationshipGroup)
		{
			updateTargets = true;
			nativeDataDirty = true;
		}

		public NPC CreateNPC(Vector3 position, Vector3 angles, float startingHealth = 50, float overloadHealth = 76, float regenerateHealth = 0.5f, float startingMana = 250, float overloadMana = 376, float regenerateMana = 7, float speed = 7f, int relationshipGroup = 0)
		{
			ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("Portal"), position, Vector3.zero);
			
			var go = Instantiate(Resources.Load<GameObject>("NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			npc.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed, relationshipGroup);

			var coll = npc.Body.BodyCollider;
			
			AlivesColliderMap[coll] = npc;
			AlivesColliderIDMap[coll.GetInstanceID()] = npc;
			
			NPCs.Add(npc);
			nativeDataDirty = true;
			return npc;
		}
		public Player CreatePlayer(Vector3 position, Vector3 angles, float startingHealth = 100, float overloadHealth = 151, float regenerateHealth = 0.5f, float startingMana = 100, float overloadMana = 151, float regenerateMana = 5, float speed = 7f, int relationshipGroup = -1)
		{
			if (Player != null)
			{
				if (Player.IsAlive)
					Player.Kill(null);
				
				Player = null;
			}
			
			var go = Instantiate(Resources.Load<GameObject>("Player"));
			go.name = "Player";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;

			go.SetActive(true);
			
			var player = go.GetComponent<Player>();
			player.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed, relationshipGroup);

			var coll = player.Body.BodyCollider;
			
			AlivesColliderMap[coll] = player;
			AlivesColliderIDMap[coll.GetInstanceID()] = player;

			Player = player;
			nativeDataDirty = true;
			return player;
		}
	}
}