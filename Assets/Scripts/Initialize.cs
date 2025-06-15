using System.IO;
using Managers;
using UI;
using UnityEngine;
using Debug = UI.Debug;

public static class Initialize
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void OnBeforeSceneLoad()
	{
		UnityEngine.Debug.Log($"Build {Application.version} at {Path.GetDirectoryName(Application.dataPath)}");
		UnityEngine.Debug.Log($"OS: {SystemInfo.operatingSystem}");
		UnityEngine.Debug.Log($"CPU: {SystemInfo.processorType} (RAM: {SystemInfo.systemMemorySize} MB)");
		UnityEngine.Debug.Log($"GPU: {SystemInfo.graphicsDeviceName} (VRAM: {SystemInfo.graphicsMemorySize} MB)");

		_ = ConsoleManager.Instance;
		_ = ObjectManager.Instance;
		_ = SelectionManager.Instance;
		_ = SettingsManager.Instance;
		_ = LocalizationManager.Instance;
		_ = GameManager.Instance;
		_ = Player.Instance;
		_ = Debug.Instance;
	}
}