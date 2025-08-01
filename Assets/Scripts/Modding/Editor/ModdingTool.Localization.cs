using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Modding.EditorScripts;
using UnityEditor;
using UnityEngine;

namespace Modding.Editor
{
	public partial class ModdingTool
	{
		private void drawLocalization()
		{
			GUILayout.BeginHorizontal();

			EditorGUILayout.LabelField($"Languages ({State.Preset.Localizations.Count})", GUILayout.Width(125));
			
			var selectLanguages = new string[State.Preset.Localizations.Count];
			
			for (var i = 0; i < selectLanguages.Length; i++)
				selectLanguages[i] = State.Preset.Localizations[i].Language;
			
			State.SelectedLanguage = EditorGUILayout.Popup(State.SelectedLanguage, selectLanguages);
			
			GUI.enabled = selectLanguages.Length > 0 && selectLanguages[State.SelectedLanguage] != "en";
			var shouldRemove = GUILayout.Button("Remove", GUILayout.Width(85));
			GUI.enabled = true;
			
			if (shouldRemove)
			{
				State.Preset.Localizations.RemoveAt(State.SelectedLanguage);
				State.SelectedLanguage = 0;
				return;
			}
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				for (var i = State.Preset.Localizations.Count - 1; i >= 0; i--)
				{
					var localization = selectLanguages[i];
					if (localization == "en")
						continue;
					
					State.Preset.Localizations.RemoveAt(i);
				}
				
				State.SelectedLanguage = 0;
				return;
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			
			State.AddLanguage = GUILayout.TextField(State.AddLanguage);
			
			GUI.enabled = !string.IsNullOrWhiteSpace(State.AddLanguage) && Array.IndexOf(selectLanguages, State.AddLanguage) == -1 && Regex.IsMatch(State.AddLanguage, "^[a-z]+$");
			var shouldAdd = GUILayout.Button("Add", GUILayout.Width(45));
			GUI.enabled = true;
			
			if (shouldAdd)
			{
				State.Preset.Localizations.Add(new LocalizationData
				{
					Language = State.AddLanguage,
					Entries = new List<LocalizationDataEntry>()
				});
				
				State.AddLanguage = "";
				State.SelectedLanguage = State.Preset.Localizations.Count - 1;
			}
			
			GUILayout.EndHorizontal();
			
			if (State.Preset.Localizations.Count == 0)
				return;

			GUILayout.Space(5);

			var list = State.Preset.Localizations[State.SelectedLanguage].Entries;
			
			EditorGUILayout.LabelField($"Entries ({State.Preset.Objects.Count * 2})");

			State.LocalizationScrollPosition = GUILayout.BeginScrollView(State.LocalizationScrollPosition);

			for (var i = 0; i < State.Preset.Objects.Count; i++)
			{
				if (i > list.Count - 1)
					list.Add(new LocalizationDataEntry());
				
				var obj = State.Preset.Objects[i];
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