using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool : EditorWindow
	{
		[SerializeField]
		public WindowState State;
		
		[MenuItem("Modding/Modding Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<ModdingTool>(true);
			window.minSize = new Vector2(350, 300);
			window.resetState();
			window.Show();
		}

		public void OnGUI()
		{
			State.Tab = GUILayout.Toolbar(State.Tab, new [] { "Presets", "Setup & Build", "Localization" });

			switch (State.Tab)
			{
				case 0:
					drawPresets();
					break;
				case 1:
					drawSetupAndBuild();
					break;
				case 2:
					drawLocalization();
					break;
			}
		}

		public void OnDestroy()
		{
			savePreset(true);
		}

		private void resetState()
		{
			State = CreateInstance<WindowState>();
			State.Preset = CreateInstance<Preset>();
		}
	}
}