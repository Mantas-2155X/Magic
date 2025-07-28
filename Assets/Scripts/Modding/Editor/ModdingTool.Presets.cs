using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool
	{
		private readonly List<Tuple<string, string>> initializedPresets = new ();

		private const string presetsPath = "Assets/Modding/Presets";
		
		private void drawPresets()
		{
			if (!State.PresetsInitialized)
				initializePresets();
			
			EditorGUILayout.LabelField($"Presets ({initializedPresets.Count})");

			State.PresetsScrollPosition = GUILayout.BeginScrollView(State.PresetsScrollPosition);

			for (var i = 0; i < initializedPresets.Count; i++)
			{
				GUILayout.BeginHorizontal();

				if (GUILayout.Button(initializedPresets[i].Item2))
					loadPreset(initializedPresets[i].Item1);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					deletePreset(initializedPresets[i].Item1);
					return;
				}
				
				GUILayout.EndHorizontal();
			}
			
			GUILayout.EndScrollView();
			
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("Save Preset"))
				savePreset();
		}

		private void savePreset(bool autoSave = false)
		{
			if (!Directory.Exists(presetsPath))
				Directory.CreateDirectory(presetsPath);

			var savePath = Path.Combine(presetsPath, autoSave ? "autosave.asset" : $"{State.Preset.Author}.{State.Preset.Name}.{State.Preset.Version}.asset");
				
			if (File.Exists(savePath))
			{
				AssetDatabase.DeleteAsset(savePath);
				Debug.LogWarning($"[ModdingTool] Removed existing preset at {savePath}");
			}
			
			AssetDatabase.CreateAsset(Instantiate(State.Preset), savePath);
			Debug.Log($"[ModdingTool] Saved preset to {savePath}");
			
			initializePresets();
		}
		
		private void loadPreset(string path)
		{
			var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
			if (preset == null)
			{
				Debug.LogWarning($"[ModdingTool] Failed to load preset at {path}");
				return;
			}
			
			State.Preset = Instantiate(preset);
			Debug.Log($"[ModdingTool] Loaded preset from {path}");

			initializePresets();
		}

		private void deletePreset(string path)
		{
			if (!File.Exists(path) || !path.EndsWith(".asset"))
			{
				Debug.LogWarning($"[ModdingTool] Failed to delete preset at {path}");
				return;
			}

			AssetDatabase.DeleteAsset(path);
			Debug.Log($"[ModdingTool] Deleted preset at {path}");
			
			initializePresets();
		}
		
		private void initializePresets()
		{
			if (!Directory.Exists(presetsPath))
				Directory.CreateDirectory(presetsPath);

			initializedPresets.Clear();
			
			var fileNames = Directory.GetFiles(presetsPath, "*.asset", SearchOption.TopDirectoryOnly);
			for (var i = 0; i < fileNames.Length; i++)
			{
				var fileInfo = new FileInfo(fileNames[i]);
				if (!fileInfo.Exists)
				{
					Debug.LogWarning($"[ModdingTool] Preset at {fileNames[i]} not found");
					continue;
				}

				initializedPresets.Add(new Tuple<string, string>(fileNames[i], fileInfo.Name[..^fileInfo.Extension.Length]));
			}
			
			State.PresetsInitialized = true;
		}
	}
}