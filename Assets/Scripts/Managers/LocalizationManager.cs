using System.Collections.Generic;
using System.IO;
using Tools;
using UI;
using Debug = UnityEngine.Debug;

namespace Managers
{
	public class LocalizationManager
	{
		private static LocalizationManager instance;
		public static LocalizationManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new LocalizationManager();
				instance.setupLanguages();
				instance.SetLanguage("en");
				
				return instance;
			}
		}

		public string CurrentLanguage { get; private set; }
		public string Path { get; private set; } = "localization";

		private readonly Dictionary<string, List<SLanguageEntry>> languages = new ();
		private readonly Dictionary<string, SLanguageEntry> currentEntries = new ();

		private readonly List<Localizer> localizers = new ();
		
		public string GetLocalizedEntry(string key)
		{
			if (!currentEntries.TryGetValue(key, out var entry))
				return key;

			return entry.Value;
		}

		public void SetLanguage(string language)
		{
			if (!languages.TryGetValue(language, out var entries))
				return;
			
			currentEntries.Clear();

			for (var i = 0; i < entries.Count; i++)
				currentEntries.Add(entries[i].Key, entries[i]);
			
			CurrentLanguage = language;

			for (var i = 0; i < localizers.Count; i++)
			{
				var localizer = localizers[i];
				if (localizer == null)
					continue;
				
				localizer.UpdateText();
			}
		}

		public void RegisterLocalizer(Localizer localizer)
		{
			if (localizer == null)
				return;

			localizer.UpdateText();
			
			localizers.AddUnique(localizer);
		}
		
		public void UnregisterLocalizer(Localizer localizer)
		{
			if (localizer == null)
				return;

			localizers.Remove(localizer);
		}
		
		private void setupLanguages()
		{
			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);

			var files = Directory.GetFiles(Path);
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];
				
				var fileInfo = new FileInfo(file);
				if (!fileInfo.Exists || fileInfo.Extension != ".tsv")
					continue;

				var name = fileInfo.Name[..^fileInfo.Extension.Length];
				var lines = File.ReadAllLines(file);

				setupLanguage(name, lines);
			}
		}

		private void setupLanguage(string language, string[] lines)
		{
			if (lines == null || lines.Length == 0)
				return;

			var entries = new List<SLanguageEntry>();

			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i];
				if (string.IsNullOrEmpty(line))
					continue;
				
				var split = line.Split('\t');
				if (split.Length != 2)
				{
					Debug.LogWarning($"[LocalizationManager] Language {language} entry {i} is wrong length");
					continue;
				}
				
				var key = split[0];
				if (string.IsNullOrEmpty(key))
				{
					Debug.LogWarning($"[LocalizationManager] Language {language} entry {i} key is invalid");
					continue;
				}
				
				var value = split[1];
				if (string.IsNullOrEmpty(value))
				{
					Debug.LogWarning($"[LocalizationManager] Language {language} entry {i} value is invalid");
					continue;
				}
				
				entries.Add(new SLanguageEntry
				{
					Key = key,
					Value = value
				});
			}
			
			languages[language] = entries;
		}

		public struct SLanguageEntry
		{
			public string Key;
			public string Value;
		}
	}
}