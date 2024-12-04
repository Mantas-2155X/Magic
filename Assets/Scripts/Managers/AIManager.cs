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

		public NPC CreateNPC(Vector3 position, Vector3 angles)
		{
			var go = Instantiate(Resources.Load<GameObject>("NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;
			
			var npc = go.GetComponent<NPC>();
			npc.Rigidbody.MovePosition(position);
			npc.Spawn(50, 100, 7f);

			NPCs.Add(npc);
			return npc;
		}

		public Player CreatePlayer(Transform spawnPoint)
		{
			return CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles);
		}
		
		public Player CreatePlayer(Vector3 position, Vector3 angles)
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
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;

			var player = go.GetComponent<Player>();
			player.Rigidbody.MovePosition(position);
			player.Spawn(100, 200, 7f);

			Player = player;
			return player;
		}
	}
}