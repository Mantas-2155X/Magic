using System;
using Managers;
using ScriptableObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World
{
	public class World : MonoBehaviour
	{
		public static World Instance;

		[SerializeField]
		public Light Flashlight;
		
		[SerializeField]
		public Transform SpawnPoints;

		[SerializeField]
		public Transform Characters;

		[SerializeField]
		public Transform Ragdolls;

		[SerializeField]
		public Transform Attacks;
		
		[SerializeField]
		public Transform Casts;

		[SerializeField]
		public Transform Projectiles;

		[SerializeField]
		public Transform Objects;

		[SerializeField]
		public Transform Decals;

		[SerializeField]
		public bool SpawnPlayer = true;

		[SerializeField]
		public bool ReloadOnPlayerDeath = true;
		
		public void Awake()
		{
			Instance = this;
		}

		public void OnDestroy()
		{
			var renderManager = RenderManager.Instance;
			if (renderManager == null)
				return;
			
			renderManager.InvertColors(0f);
		}

		public void Start()
		{
			if (!SpawnPlayer)
				return;
			
			var spawnPoint = SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount));
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("AI_PLAYER_NAME"));
		}
	}
}