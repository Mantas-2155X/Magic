using System.Collections.Generic;
using System.IO;
using Managers;
using Modding;
using UI;
using UnityEngine;
using Debug = UI.Debug;

public static class Initialize
{
#if UNITY_EDITOR
	private static readonly List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries = new ();
#endif
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void OnBeforeSceneLoad()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.playModeStateChanged += onPlayModeStateChanged;
		
		entries.Clear();

		var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
		var group = settings.DefaultGroup;

		foreach (var entry in group.entries)
			entries.Add(entry);

		for (var i = entries.Count - 1; i >= 0; i--)
			group.RemoveAssetEntry(entries[i], false);
#endif
		UnityEngine.Debug.Log($"Build {Application.version} at {Path.GetDirectoryName(Application.dataPath)}");
		UnityEngine.Debug.Log($"OS: {SystemInfo.operatingSystem}");
		UnityEngine.Debug.Log($"CPU: {SystemInfo.processorType} (RAM: {SystemInfo.systemMemorySize} MB)");
		UnityEngine.Debug.Log($"GPU: {SystemInfo.graphicsDeviceName} (VRAM: {SystemInfo.graphicsMemorySize} MB)");

		_ = ConsoleManager.Instance;
		_ = ModLoader.Instance;
		_ = ObjectManager.Instance;
		_ = SelectionManager.Instance;
		_ = SettingsManager.Instance;
		_ = LocalizationManager.Instance;
		_ = GameManager.Instance;
		_ = Player.Instance;
		_ = Debug.Instance;
	}
#if UNITY_EDITOR
	private static void onPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
	{
		switch (state)
		{
			case UnityEditor.PlayModeStateChange.ExitingPlayMode:
			{
				var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
				var group = settings.DefaultGroup;

				for (var i = 0; i < entries.Count; i++)
					settings.MoveEntry(entries[i], group, false, false);
				
				entries.Clear();
				break;
			}
		}
	}	
#endif
}