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
				
				TimeScale = 1f;
				TargetFPS = 10000;
				
				return instance;
			}
		}

		public static float TimeScale
		{
			get => Time.timeScale;
			set
			{
				var changeTo = value;
				changeTo = Mathf.Clamp(changeTo, 0f, 100f);
				Time.timeScale = changeTo;
			}
		}
		
		public static int TargetFPS
		{
			get => Application.targetFrameRate;
			set
			{
				var changeTo = value;
				changeTo = Mathf.Clamp(changeTo, 10, 10000);
				Application.targetFrameRate = changeTo;
			}
		}
	}
}