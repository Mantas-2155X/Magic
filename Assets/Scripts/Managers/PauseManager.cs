using Managers.Events;
using UnityEngine;

namespace Managers
{
	public class PauseManager
	{
		private static PauseManager instance;
		public static PauseManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new PauseManager();
				return instance;
			}
		}

		public readonly OnPausedEvent OnPausedEvent = new ();
		public readonly OnUnpausedEvent OnUnpausedEvent = new ();
		
		public static bool IsPaused { get; private set; }
		
		public void Pause()
		{
			if (IsPaused)
				return;
			
			IsPaused = true;
			OnPausedEvent?.Invoke();
			
			Time.timeScale = 0f;
		}

		public void Unpause()
		{
			if (!IsPaused)
				return;
			
			IsPaused = false;
			OnUnpausedEvent?.Invoke();
			
			Time.timeScale = GameManager.TimeScale;
		}
	}
}