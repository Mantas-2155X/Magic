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
		
		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			AIManager.Instance.CreatePlayer(SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount)));
		}
	}
}