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
		public float TimeScale = 1f;

		[SerializeField]
		public int TargetFPS = 1000;

		private float previousTimeScale;
		private int previousTargetFPS;
		
		public void Awake()
		{
			Instance = this;
		}
		
		public void OnDisable()
		{
			TimeScale = 1f;
		}

		public void Start()
		{
			var spawnPoint = SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount));
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("Player"));
		}
		
		public void Update()
		{
			if (TimeScale != previousTimeScale)
			{
				previousTimeScale = TimeScale;
				Time.timeScale = TimeScale;
			}

			if (TargetFPS != previousTargetFPS)
			{
				previousTargetFPS = TargetFPS;
				Application.targetFrameRate = TargetFPS;
			}
		}
	}
}