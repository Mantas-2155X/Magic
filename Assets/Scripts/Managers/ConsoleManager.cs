using System;
using System.Collections.Generic;
using System.Globalization;
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

		public void AddCommand(string name, string description, EConsoleCommandParameter[] parameters, UnityAction<object[]> action)
		{
			for (var i = commands.Count - 1; i >= 0; i--)
			{
				if (commands[i].Name != name)
					continue;

				Debug.LogWarning("[ConsoleManager] Command with the same name already exists");
				return;
			}
			
			commands.Add(new SConsoleCommand(name, description, parameters, action));
		}
		
		public void AddCommand(string name, string description, UnityAction<object[]> action)
		{
			AddCommand(name, description, null, action);
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

		public EConsoleCommandResult ExecuteCommand(string name)
		{
			if (string.IsNullOrEmpty(name))
				return EConsoleCommandResult.NotFound;
			
			var split = name.Split(" ");
			if (split.Length == 0)
				return EConsoleCommandResult.NotFound;
			
			for (var i = 0; i < commands.Count; i++)
			{
				var command = commands[i];
				if (command.Name != split[0])
					continue;

				object[] inputParameters = null;
				
				var commandParameters = command.Parameters;
				if (commandParameters != null)
				{
					if (commandParameters.Length != split.Length - 1)
						return EConsoleCommandResult.IncorrectUsage;

					inputParameters = new object[commandParameters.Length];
					
					for (var k = 0; k < commandParameters.Length; k++)
					{
						var commandParameter = commandParameters[k];
						switch (commandParameter)
						{
							case EConsoleCommandParameter.String:
								inputParameters[k] = split[k + 1];
								break;
							case EConsoleCommandParameter.Float:
								if (!float.TryParse(split[k + 1], NumberStyles.Float, CultureInfo.CurrentCulture, out var floatValue))
									return EConsoleCommandResult.IncorrectUsage;
								inputParameters[k] = floatValue;
								break;
							case EConsoleCommandParameter.Int:
								if (!int.TryParse(split[k + 1], NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue))
									return EConsoleCommandResult.IncorrectUsage;
								inputParameters[k] = intValue;
								break;
							default:
								throw new NotImplementedException();
						}
					}
				}
				
				command.Action(inputParameters);
				return EConsoleCommandResult.Success;
			}

			return EConsoleCommandResult.NotFound;
		}
		
		private void setupCommands()
		{
			AddCommand("quit", "Quit the game", _ =>
			{
				SceneManager.Instance.ChangeScene("Exit", true, false, false);
			});
			
			AddCommand("title", "Return to title", _ =>
			{
				SceneManager.Instance.ChangeScene("Scenes/Title", true, true, false);
			});
			
			AddCommand("timescale", "Sets the time scale", new [] {EConsoleCommandParameter.Float}, args =>
			{
				var world = World.World.Instance;
				if (world == null)
					return;

				var value = (float)args[0];
				value = Mathf.Clamp(value, 0f, 100f);
				
				world.TimeScale = value;
			});

			AddCommand("clear", "Clears the console", _ =>
			{
				ClearEntries();
			});
			
			AddCommand("kill", "Kills the player", _ =>
			{
				var player = AIManager.Instance.Player;
				if (player == null && player.IsAlive)
					return;
				
				player.Kill(null);
			});
			
			AddCommand("killall", "Kills everyone alive", _ =>
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
			
			AddCommand("help", "Lists all commands", _ =>
			{
				AddEntry(EConsoleEntryType.Info, "Available Commands:");
				
				for (var i = 0; i < commands.Count; i++)
				{
					var command = commands[i];
					var parameters = "";
					
					var commandParameters = command.Parameters;
					if (commandParameters != null)
					{
						parameters += "(";
						
						for (var k = 0; k < commandParameters.Length; k++)
						{
							parameters += commandParameters[k];

							if (k != commandParameters.Length - 1)
								parameters += ", ";
						}
						
						parameters += ") ";
					}
					
					AddEntry(EConsoleEntryType.Info, $"{command.Name} {parameters}- {command.Description}");
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

		public enum EConsoleEntryType
		{
			Info,
			Warning,
			Error
		}
		
		public struct SConsoleCommand
		{
			public string Name { get; private set; }

			public string Description { get; private set; }
			
			public EConsoleCommandParameter[] Parameters { get; private set; }

			public UnityAction<object[]> Action { get; private set; }

			public SConsoleCommand(string name, string description, EConsoleCommandParameter[] parameters, UnityAction<object[]> action)
			{
				Name = name;
				Description = description;
				Parameters = parameters;
				Action = action;
			}
		}

		public enum EConsoleCommandParameter
		{
			String,
			Float,
			Int,
		}

		public enum EConsoleCommandResult
		{
			NotFound,
			Success,
			IncorrectUsage
		}
	}
}