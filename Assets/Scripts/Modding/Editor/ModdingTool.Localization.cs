using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool
	{
		private int selectedLanguage;
		private string addLanguage;

		private Vector2 localizationScrollPosition;
		
		private void drawLocalization()
		{
			GUILayout.BeginHorizontal();

			EditorGUILayout.LabelField($"Languages ({CurrentPreset.Localizations.Count})", GUILayout.Width(125));
			
			var selectLanguages = new string[CurrentPreset.Localizations.Count];
			
			for (var i = 0; i < selectLanguages.Length; i++)
				selectLanguages[i] = CurrentPreset.Localizations[i].Language;
			
			selectedLanguage = EditorGUILayout.Popup(selectedLanguage, selectLanguages);
			
			GUI.enabled = selectLanguages.Length > 0;
			var shouldRemove = GUILayout.Button("Remove", GUILayout.Width(85));
			GUI.enabled = true;
			
			if (shouldRemove)
			{
				CurrentPreset.Localizations.RemoveAt(selectedLanguage);
				selectedLanguage = 0;
				return;
			}
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				CurrentPreset.Localizations.Clear();
				return;
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			
			addLanguage = GUILayout.TextField(addLanguage);
			
			GUI.enabled = !string.IsNullOrWhiteSpace(addLanguage) && Array.IndexOf(selectLanguages, addLanguage) == -1 && Regex.IsMatch(addLanguage, "^[a-z]+$");
			var shouldAdd = GUILayout.Button("Add", GUILayout.Width(45));
			GUI.enabled = true;
			
			if (shouldAdd)
			{
				CurrentPreset.Localizations.Add(new LocalizationData
				{
					Language = addLanguage,
					Entries = new List<LocalizationDataEntry>()
				});
				
				addLanguage = "";
				selectedLanguage = CurrentPreset.Localizations.Count - 1;
			}
			
			GUILayout.EndHorizontal();
			
			if (CurrentPreset.Localizations.Count == 0)
				return;

			GUILayout.Space(5);

			var list = CurrentPreset.Localizations[selectedLanguage].Entries;
			
			EditorGUILayout.LabelField($"Entries ({CurrentPreset.Objects.Count * 2})");

			localizationScrollPosition = GUILayout.BeginScrollView(localizationScrollPosition);

			for (var i = 0; i < CurrentPreset.Objects.Count; i++)
			{
				if (i > list.Count - 1)
					list.Add(new LocalizationDataEntry());
				
				var obj = CurrentPreset.Objects[i];
				if (obj == null)
					continue;

				EditorGUIUtility.labelWidth = position.width / 2f - 10;
				
				var localizationEntry = list[i];
				localizationEntry.Name = EditorGUILayout.TextField(obj.Name, localizationEntry.Name);
				localizationEntry.Description = EditorGUILayout.TextField(obj.Description, localizationEntry.Description);
				
				EditorGUIUtility.labelWidth = 0;

				GUILayout.Space(5);
			}
			
			GUILayout.EndScrollView();
		}
	}
}