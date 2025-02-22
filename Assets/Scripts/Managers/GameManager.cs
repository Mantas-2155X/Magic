using UnityEngine;

namespace Managers
{
	public class GameManager
	{
		private static GameManager instance;
		public static GameManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new GameManager();
				return instance;
			}
		}

		private static float timeScale = 1f;
		public static float TimeScale
		{
			get => timeScale;
			set
			{
				timeScale = Mathf.Clamp(value, 0f, 100f);
				
				if (!PauseManager.IsPaused)
					Time.timeScale = timeScale;
			}
		}
	}
}