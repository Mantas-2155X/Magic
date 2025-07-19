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
		
		public virtual void Awake()
		{
			Instance = this;
			
			// this has to be resources because addressables deadlocks while loading an addressable scene
			
			if (!GameObject.Find("EventSystem"))
				Instantiate(Resources.Load("Scene/EventSystem"));

			if (!GameObject.Find("Main Camera"))
				Instantiate(Resources.Load("Scene/Main Camera"));
			
			if (!GameObject.Find("Managers"))
				Instantiate(Resources.Load("Scene/Managers"));
		}

		public virtual void OnDestroy()
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

		public virtual void Start()
		{
			if (!SpawnPlayer)
				return;
			
			var spawnPoint = SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount));
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("AI_PLAYER_NAME"));
		}
	}
}