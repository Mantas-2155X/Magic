using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool
	{
		[SerializeField]
		public Preset CurrentPreset;

		private Vector2 presetsScrollPosition;

		private readonly List<Tuple<string, string>> initializedPresets = new ();

		private readonly string presetsPath = "Assets/Modding/Presets";
		
		private void drawPresets()
		{
			EditorGUILayout.LabelField($"Presets ({initializedPresets.Count})");

			presetsScrollPosition = GUILayout.BeginScrollView(presetsScrollPosition);

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

			var savePath = Path.Combine(presetsPath, autoSave ? "autosave.asset" : $"{CurrentPreset.Author}.{CurrentPreset.Name}.{CurrentPreset.Version}.asset");
				
			if (File.Exists(savePath))
			{
				AssetDatabase.DeleteAsset(savePath);
				Debug.LogWarning($"[ModdingTool] Removed existing preset at {savePath}");
			}
			
			AssetDatabase.CreateAsset(Instantiate(CurrentPreset), savePath);
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
			
			CurrentPreset = Instantiate(preset);
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
		}
	}
}