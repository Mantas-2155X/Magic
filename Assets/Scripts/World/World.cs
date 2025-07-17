using Managers;
using ScriptableObjects;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace World
{
	public class World : MonoBehaviour
	{
		public static World Instance;

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
			
			if (!GameObject.Find("EventSystem"))
				Addressables.InstantiateAsync("Assets/Prefabs/Scene/EventSystem.prefab").WaitForCompletion();

			if (!GameObject.Find("Main Camera"))
				Addressables.InstantiateAsync("Assets/Prefabs/Scene/Main Camera.prefab").WaitForCompletion();
			
			if (!GameObject.Find("Managers"))
				Addressables.InstantiateAsync("Assets/Prefabs/Scene/Managers.prefab").WaitForCompletion();
		}

		public void OnDestroy()
		{
			var player = Player.Instance;
			if (player != null)
				player.Notice.ClearMessage();

			var renderManager = RenderManager.Instance;
			if (renderManager == null)
				return;
			
			renderManager.InvertColors(0f);
			renderManager.Desaturate(false);
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