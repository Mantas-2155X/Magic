using System.Collections.Generic;
using Managers.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Managers
{
	public class ConsoleManager
	{
		private static ConsoleManager instance;
		public static ConsoleManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new ConsoleManager();
				instance.setupCommands();
				return instance;
			}
		}

		public static readonly OnConsoleEntryAddedEvent OnConsoleEntryAddedEvent = new ();
		public static readonly OnConsoleClearedEvent OnConsoleClearedEvent = new ();

		private readonly List<SConsoleEntry> entries = new ();
		private readonly List<SConsoleCommand> commands = new ();

		#region Entries

		public void AddEntry(EConsoleEntryType type, string text)
		{
			var entry = new SConsoleEntry(type, text);
			entries.Add(entry);
			
			OnConsoleEntryAddedEvent?.Invoke(entry);
		}

		public void RemoveEntry(int index)
		{
			entries.RemoveAt(index);
		}

		public void ClearEntries()
		{
			entries.Clear();
			
			OnConsoleClearedEvent?.Invoke();
		}

		public List<SConsoleEntry> GetEntries()
		{
			return entries;
		}

		#endregion

		#region Commands

		public void AddCommand(string name, string description, UnityAction action)
		{
			for (var i = commands.Count - 1; i >= 0; i--)
			{
				if (commands[i].Name != name)
					continue;

				Debug.LogWarning("[ConsoleManager] Command with the same name already exists");
				return;
			}
			
			commands.Add(new SConsoleCommand(name, description, action));
		}

		public void RemoveCommand(string name)
		{
			for (var i = commands.Count - 1; i >= 0; i--)
			{
				if (commands[i].Name != name)
					continue;

				commands.RemoveAt(i);
				return;
			}
		}

		public void ClearCommands()
		{
			commands.Clear();
		}

		public bool ExecuteCommand(string name)
		{
			for (var i = 0; i < commands.Count; i++)
			{
				var command = commands[i];
				if (command.Name != name)
					continue;

				command.Action();
				return true;
			}

			return false;
		}
		
		private void setupCommands()
		{
			AddCommand("quit", "Quit the game", () =>
			{
				SceneManager.Instance.ChangeScene("Exit", true, false, false);
			});
			
			AddCommand("title", "Return to title", () =>
			{
				SceneManager.Instance.ChangeScene("Scenes/Title", true, true, false);
			});
			
			AddCommand("clear", "Clears the console", () =>
			{
				ClearEntries();
			});
			
			AddCommand("kill", "Kills the player", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null && player.IsAlive)
					return;
				
				player.Kill(null);
			});
			
			AddCommand("killall", "Kills everyone alive", () =>
			{
				var player = AIManager.Instance.Player;
				if (player != null && player.IsAlive)
					player.Kill(null);

				var npcs = AIManager.Instance.NPCs;
				for (var i = 0; i < npcs.Count; i++)
				{
					var npc = npcs[i];
					if (npc == null || !npc.IsAlive)
						continue;
					
					npc.Kill(null);
				}
			});
			
			AddCommand("help", "Lists all commands", () =>
			{
				AddEntry(EConsoleEntryType.Info, "Available Commands:");
				
				for (var i = 0; i < commands.Count; i++)
				{
					var command = commands[i];
					AddEntry(EConsoleEntryType.Info, $"{command.Name} - {command.Description}");
				}
			});
		}

		#endregion
		
		public struct SConsoleEntry
		{
			public EConsoleEntryType Type { get; private set; }

			public string Text { get; private set; }

			public SConsoleEntry(EConsoleEntryType type, string text)
			{
				Type = type;
				Text = text;
			}
		}

		public struct SConsoleCommand
		{
			public string Name { get; private set; }

			public string Description { get; private set; }
			
			public UnityAction Action { get; private set; }

			public SConsoleCommand(string name, string description, UnityAction action)
			{
				Name = name;
				Description = description;
				Action = action;
			}
		}

		public enum EConsoleEntryType
		{
			Info,
			Warning,
			Error
		}
	}
}