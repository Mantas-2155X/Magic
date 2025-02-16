using Managers;
using UI;
using UnityEngine;
using Debug = UI.Debug;

public static class Initialize
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void OnBeforeSceneLoad()
	{
		_ = SelectionManager.Instance;
		_ = ConsoleManager.Instance;
		_ = SettingsManager.Instance;
		_ = LocalizationManager.Instance;
		_ = GameManager.Instance;
		_ = Player.Instance;
		_ = Debug.Instance;
	}
}