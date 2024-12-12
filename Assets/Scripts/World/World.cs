using Managers;
using UnityEngine;
using Random = UnityEngine.Random;

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
		public Transform Projectiles;

		[SerializeField]
		public Transform Impacts;

		[SerializeField]
		public Transform Other;

		[SerializeField]
		public Transform Objects;
		
		[SerializeField]
		public float TimeScale = 1f;

		private float previousTimeScale;
		
		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			var spawnPoint = SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount));
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles);
		}
		
		public void Update()
		{
			if (TimeScale == previousTimeScale)
				return;

			previousTimeScale = TimeScale;
			Time.timeScale = TimeScale;
		}
	}
}