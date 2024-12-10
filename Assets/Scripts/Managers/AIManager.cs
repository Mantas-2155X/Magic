using System.Collections.Generic;
using AI;
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
		
		public void Awake()
		{
			Instance = this;
		}

		public NPC CreateNPC(Transform spawnPoint, float health = 50, float overload = 100, float mana = 250, float overloadMana = 500, float speed = 7f)
		{
			return CreateNPC(spawnPoint.position, spawnPoint.eulerAngles, health, overload, mana, overloadMana, speed);
		}
		
		public NPC CreateNPC(Vector3 position, Vector3 angles, float health = 50, float overload = 100, float mana = 250, float overloadMana = 500, float speed = 7f)
		{
			var go = Instantiate(Resources.Load<GameObject>("Alives/NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;
			
			var npc = go.GetComponent<NPC>();
			npc.Body.Rigidbody.MovePosition(position);
			npc.Spawn(health, overload, mana, overloadMana, speed);

			NPCs.Add(npc);
			return npc;
		}

		public Player CreatePlayer(Transform spawnPoint, float health = 100, float overload = 200, float mana = 100, float overloadMana = 200, float speed = 7f)
		{
			return CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, health, overload, mana, overloadMana, speed);
		}
		
		public Player CreatePlayer(Vector3 position, Vector3 angles, float health = 100, float overload = 200, float mana = 100, float overloadMana = 200, float speed = 7f)
		{
			if (Player != null)
			{
				if (Player.IsAlive)
					Player.Kill(null);
				
				Player = null;
			}
			
			var go = Instantiate(Resources.Load<GameObject>("Alives/Player"));
			go.name = "Player";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;

			var player = go.GetComponent<Player>();
			player.Body.Rigidbody.MovePosition(position);
			player.Spawn(health, overload, mana, overloadMana, speed);

			Player = player;
			return player;
		}
	}
}