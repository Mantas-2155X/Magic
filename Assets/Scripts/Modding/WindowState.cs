using System;
using UnityEngine;

namespace Modding
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

#if UNITY_EDITOR
		public static void Save(WindowState state)
		{
			UnityEditor.AssetDatabase.CreateAsset(state.Preset, "Assets/preset.asset");
			UnityEditor.AssetDatabase.CreateAsset(state, "Assets/window.asset");
		}

		public static WindowState Load()
		{
			var state = Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<WindowState>("Assets/window.asset"));
			state.Preset = Instantiate(state.Preset);
			
			return state;
		}

		public static void Delete()
		{
			UnityEditor.AssetDatabase.DeleteAsset("Assets/window.asset");
			UnityEditor.AssetDatabase.DeleteAsset("Assets/preset.asset");
		}
#endif
	}
}