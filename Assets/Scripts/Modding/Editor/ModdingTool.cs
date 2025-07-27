using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool : EditorWindow
	{
		[SerializeField]
		public int CurrentTab = 1;
		
		[MenuItem("Modding/Modding Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<ModdingTool>(true);
			window.minSize = new Vector2(350, 300);
			window.Show();
			
			window.initializePresets();
		}

		public void OnGUI()
		{
			CurrentTab = GUILayout.Toolbar(CurrentTab, new [] { "Presets", "Setup & Build", "Localization" });

			if (CurrentPreset == null)
				CurrentPreset = CreateInstance<Preset>();
			
			switch (CurrentTab)
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
	}
}