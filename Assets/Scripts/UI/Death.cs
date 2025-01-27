using AI;
using AI.Base;
using AI.Interfaces;
using Managers;
using UnityEngine;

namespace UI
{
	public class Death : MonoBehaviour
	{
		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnDestroy()
		{
			BaseAlive.OnDeathEvent.RemoveListener(OnDeath);
			BaseAlive.OnSpawnEvent.RemoveListener(OnSpawn);
		}

		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not AI.Player)
				return;
			
			gameObject.SetActive(true);
			SceneManager.Instance.ReloadScene(true, true, true, 1f);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not AI.Player)
				return;

			gameObject.SetActive(false);
		}
	}
}