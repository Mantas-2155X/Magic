using Managers;
using UnityEngine;

namespace World
{
	public class World : MonoBehaviour
	{
		public static World Instance;

		[SerializeField]
		public Light Sun;
		
		[SerializeField]
		public Water Water;

		[SerializeField]
		public Terrain Terrain;
		
		[SerializeField]
		public Transform SpawnPoints;

		[SerializeField]
		public Transform Characters;

		[SerializeField]
		public Transform Ragdolls;

		[SerializeField]
		public Transform Dropped;

		[SerializeField]
		public float AdditionalGravity = -0.1f;
		
		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			AIManager.Instance.CreatePlayer(SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount)));
		}
		
		public void FixedUpdate()
		{
			// Simulate an additional gravity that affects all beings
			
			var aiManager = AIManager.Instance;
			var npcs = aiManager.NPCs;
			var player = aiManager.Player;

			if (player.IsAlive && !player.IsNoclip)
				player.Rigidbody.AddForce(0, AdditionalGravity, 0, ForceMode.VelocityChange);

			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc.Agent.enabled || npc.IsNoclip)
					continue;

				npc.Rigidbody.AddForce(0, AdditionalGravity, 0, ForceMode.VelocityChange);
			}
		}
	}
}