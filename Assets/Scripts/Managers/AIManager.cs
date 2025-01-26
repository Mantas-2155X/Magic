using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Enums;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using ScriptableObjects;
using UnityEngine;

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
		
		private bool updateTargets;
		
		public void Awake()
		{
			Instance = this;
			
			BaseAlive.OnDamageEvent.AddListener(onDamage);
			BaseAlive.OnSpawnEvent.AddListener(onSpawn);
			BaseAlive.OnDeathEvent.AddListener(onDeath);
			BaseAlive.OnRelationshipGroupChangedEvent.AddListener(onRelationshipGroupChanged);
		}

		public void OnDestroy()
		{
			BaseAlive.OnDamageEvent.RemoveListener(onDamage);
			BaseAlive.OnSpawnEvent.RemoveListener(onSpawn);
			BaseAlive.OnDeathEvent.RemoveListener(onDeath);
			BaseAlive.OnRelationshipGroupChangedEvent.RemoveListener(onRelationshipGroupChanged);
		}

		public void Update()
		{
			handleTargets();
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

				if (npc.AttackTarget is not IAlive target)
					continue;

				if (!target.IsAlive || npc.RelationshipGroup == target.RelationshipGroup)
					npc.AssignAttackTarget(null);
			}
		}

		private void onDamage(IAlive alive, float damage, object source, EElement type)
		{
			if (!alive.IsAlive || alive is not NPC npc)
				return;

			// Should work for both aggressive and passive aggressive
			if (((NPCData)npc.Data).TargetMode == ETargetMode.Passive)
				return;
			
			IAlive aggressor = null;
			
			switch (source)
			{
				case IAlive aggr:
					aggressor = aggr;
					break;
				case ISpell spell:
					aggressor = spell.Owner;
					break;
				case IAttack attack:
					aggressor = attack.GetAlive();
					break;
				case IProjectile projectile:
					aggressor = projectile.GetAlive();
					break;
			}
			
			// Don't attack allies or dead targets
			if (aggressor == null || !aggressor.IsAlive || aggressor.RelationshipGroup == alive.RelationshipGroup)
				return;

			// Make sure its within sense or spell range
			if (!npc.WithinRange.SenseDistanceCheck(aggressor.GetTransform(), false, true))
				return;
			
			npc.AssignAttackTarget((Component)aggressor);
		}

		private void onSpawn(IAlive alive)
		{
			updateTargets = true;
		}
		
		private void onDeath(IAlive alive, object source)
		{
			updateTargets = true;
		}

		private void onRelationshipGroupChanged(IAlive alive, int previousRelationshipGroup, int newRelationshipGroup)
		{
			updateTargets = true;
		}

		public NPC CreateNPC(Vector3 position, Vector3 angles, NPCData data, int relationshipGroup = 0)
		{
			ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("OBJECT_PORTAL_NAME"), position, Vector3.zero);
			
			var go = Instantiate(data.Prefab);
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			
			AlivesColliderMap[npc.Body.BodyCollider] = npc;
			NPCs.Add(npc);

			npc.Spawn(data, relationshipGroup);
			return npc;
		}
		public Player CreatePlayer(Vector3 position, Vector3 angles, PlayerData data, int relationshipGroup = -1)
		{
			if (Player != null)
			{
				if (Player.IsAlive)
					Player.Kill(null);
				
				Player = null;
			}
			
			var go = Instantiate(data.Prefab);
			go.name = "Player";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;

			go.SetActive(true);
			
			var player = go.GetComponent<Player>();
			
			AlivesColliderMap[player.Body.BodyCollider] = player;
			Player = player;

			player.Spawn(data, relationshipGroup);
			return player;
		}
	}
}