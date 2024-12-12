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
		
		public NPC CreateNPC(Vector3 position, Vector3 angles, float startingHealth = 50, float overloadHealth = 76, float regenerateHealth = 0.5f, float startingMana = 250, float overloadMana = 376, float regenerateMana = 7, float speed = 7f)
		{
			ObjectManager.Instance.CreatePortal(position);
			
			var go = Instantiate(Resources.Load<GameObject>("Alives/NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			npc.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			NPCs.Add(npc);
			return npc;
		}
		public Player CreatePlayer(Vector3 position, Vector3 angles, float startingHealth = 100, float overloadHealth = 151, float regenerateHealth = 0.5f, float startingMana = 100, float overloadMana = 151, float regenerateMana = 5, float speed = 7f)
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

			go.SetActive(true);
			
			var player = go.GetComponent<Player>();
			player.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			Player = player;
			return player;
		}
	}
}