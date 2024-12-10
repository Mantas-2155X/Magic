using AI;
using AI.Base;
using AI.Interfaces;
using Managers;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace UI
{
	public class Death : MonoBehaviour
	{
		public Button Respawn;

		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			gameObject.SetActive(true);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player)
				return;

			gameObject.SetActive(false);
		}
		
		public void OnRespawnClicked()
		{
			var spawnPoint = World.World.Instance.SpawnPoints.GetChild(Random.Range(0, World.World.Instance.SpawnPoints.childCount));
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles);
		}
	}
}