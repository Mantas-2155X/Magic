#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Modding.EditorScripts
{
	[Serializable]
	public class WindowState : ScriptableObject
	{
		[SerializeField]
		public int Tab = 1;

		#region Presets

		[SerializeField]
		public Preset Preset;

		[SerializeField]
		public Vector2 PresetsScrollPosition;

		[NonSerialized]
		public bool PresetsInitialized;
		
		#endregion
		
		#region Setup And Build

		[SerializeField]
		public Vector2 SetupAndBuildScrollPosition;

		#endregion
		
		#region Localization

		[SerializeField]
		public int SelectedLanguage;

		[SerializeField]
		public string AddLanguage;

		[SerializeField]
		public Vector2 LocalizationScrollPosition;
		
		#endregion

		private const string statePath = "Assets/Modding";
		
		public static void Save(WindowState state)
		{
			if (!Directory.Exists(statePath))
				Directory.CreateDirectory(statePath);
			
			AssetDatabase.CreateAsset(state.Preset, Path.Combine(statePath, "preset.asset"));
			AssetDatabase.CreateAsset(state, Path.Combine(statePath, "window.asset"));
		}

		public static WindowState Load()
		{
			var loadPath = Path.Combine(statePath, "window.asset");
			
			if (!File.Exists(loadPath))
			{
				Debug.LogWarning("[WindowState] State asset not found");
				return null;
			}

			var state = Instantiate(AssetDatabase.LoadAssetAtPath<WindowState>(loadPath));
			state.Preset = Instantiate(state.Preset);
			
			return state;
		}

		public static void Delete()
		{
			AssetDatabase.DeleteAsset(Path.Combine(statePath, "window.asset"));
			AssetDatabase.DeleteAsset(Path.Combine(statePath, "preset.asset"));
		}
	}
}
#endif