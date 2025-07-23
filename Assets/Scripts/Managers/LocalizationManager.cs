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
		public string Path { get; private set; } = "data/localization";

		private readonly Dictionary<string, List<SLanguageEntry>> languages = new ();
		private readonly Dictionary<string, SLanguageEntry> currentEntries = new ();

		private readonly List<Localizer> localizers = new ();
		
		public string GetLocalizedEntry(string key)
		{
			if (!currentEntries.TryGetValue(key, out var entry))
				return key;

			return entry.Value;
		}

		public bool AddLocalizedEntry(string key, string value, string language)
		{
			if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
				return false;
			
			if (!languages.TryGetValue(language, out var entries))
				return false;

			for (var i = 0; i < entries.Count; i++)
			{
				var entry = entries[i];
				if (entry.Key != key)
					continue;

				return false;
			}

			var newEntry = new SLanguageEntry
			{
				Key = key,
				Value = value,
			};
			
			entries.Add(newEntry);
			
			if (CurrentLanguage == language)
				currentEntries[key] = newEntry;
			
			return true;
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
				
				localizer.Apply();
			}
		}

		public void RegisterLocalizer(Localizer localizer)
		{
			if (localizer == null)
				return;

			localizer.Apply();
			
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

			var directories = Directory.GetDirectories(Path);
			for (var i = 0; i < directories.Length; i++)
			{
				var directory = directories[i];
				
				var directoryInfo = new DirectoryInfo(directory);
				if (!directoryInfo.Exists)
					continue;

				var files = Directory.GetFiles(directory, "*.tsv", SearchOption.AllDirectories);
				var lines = new List<string>();
				
				for (var k = 0; k < files.Length; k++)
				{
					var file = files[k];
					
					var fileinfo = new FileInfo(file);
					if (!fileinfo.Exists)
						continue;

					lines.AddRange(File.ReadAllLines(file));
				}
				
				setupLanguage(directoryInfo.Name, lines);
			}
		}

		private void setupLanguage(string language, List<string> lines)
		{
			if (lines == null || lines.Count == 0)
				return;

			var entries = new List<SLanguageEntry>();

			for (var i = 0; i < lines.Count; i++)
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