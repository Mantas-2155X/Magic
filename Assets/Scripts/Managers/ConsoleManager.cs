using System;
using System.Collections.Generic;
using System.Globalization;
using AI.Enums;
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

		public void AddCommand(string name, string description, EConsoleCommandParameter[] parameters, UnityAction<object[]> parametersAction, UnityAction basicAction)
		{
			for (var i = commands.Count - 1; i >= 0; i--)
			{
				if (commands[i].Name != name)
					continue;

				Debug.LogWarning("[ConsoleManager] Command with the same name already exists");
				return;
			}
			
			commands.Add(new SConsoleCommand(name, description, parameters, parametersAction, basicAction));
		}
		
		public void AddCommand(string name, string description, EConsoleCommandParameter[] parameters, UnityAction<object[]> parametersAction)
		{
			AddCommand(name, description, parameters, parametersAction, null);
		}
		
		public void AddCommand(string name, string description, UnityAction basicAction)
		{
			AddCommand(name, description, null, null, basicAction);
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
			{
				// No command specified, fail
				return EConsoleCommandResult.NotFound;
			}
			
			var split = name.Split(" ");
			
			var newSplit = new List<string>();
			for (var i = 0; i < split.Length; i++)
			{
				var entry = split[i];
				
				// Remove empty or space characters from the parameters
				if (string.IsNullOrWhiteSpace(entry))
					continue;
				
				newSplit.Add(entry);
			}
			split = newSplit.ToArray();
			
			if (split.Length == 0)
			{
				// No command specified, fail
				return EConsoleCommandResult.NotFound;
			}

			for (var i = 0; i < commands.Count; i++)
			{
				var command = commands[i];
				if (command.Name != split[0])
					continue;

				var commandParameters = command.Parameters;
				if (commandParameters == null)
				{
					if (split.Length > 1)
					{
						// No parameters in command but there are some in the input, fail
						return EConsoleCommandResult.IncorrectUsage;
					}
					
					// No parameters in command and input, run the basic action
					command.BasicAction();
					return EConsoleCommandResult.Success;
				}
				
				if (commandParameters.Length != split.Length - 1)
				{
					if (split.Length - 1 != 0 || command.BasicAction == null)
					{
						// Command has parameters but input doesn't and there isn't a basic action, fail
						return EConsoleCommandResult.IncorrectUsage;
					}

					// Command has parameters but input doesn't, run the basic action
					command.BasicAction();
					return EConsoleCommandResult.Success;
				}

				var inputParameters = new object[commandParameters.Length];
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
							{
								// Command parameter should be a float but the input parameter isn't, fail
								return EConsoleCommandResult.IncorrectUsage;
							}
							inputParameters[k] = floatValue;
							break;
						case EConsoleCommandParameter.Int:
							if (!int.TryParse(split[k + 1], NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue))
							{
								// Command parameter should be an int but the input parameter isn't, fail
								return EConsoleCommandResult.IncorrectUsage;
							}
							inputParameters[k] = intValue;
							break;
						case EConsoleCommandParameter.Bool:
							if (!bool.TryParse(split[k + 1], out var boolValue))
							{
								// Command parameter should be a bool but the input parameter isn't, fail
								return EConsoleCommandResult.IncorrectUsage;
							}
							inputParameters[k] = boolValue;
							break;
						default:
							throw new NotImplementedException();
					}
				}
				
				// Command and input parameters are matched, run the arguments action
				command.ArgumentsAction(inputParameters);
				return EConsoleCommandResult.Success;
			}

			// No command found, fail
			return EConsoleCommandResult.NotFound;
		}
		
		private void setupCommands()
		{
			AddCommand("quit", "Quit the game", () =>
			{
				SceneManager.Instance.ChangeScene("Exit", true, false, false);
			});
			
			AddCommand("title", "Return to title", () =>
			{
				SceneManager.Instance.ChangeScene("Title", true, true, false);
			});
			
			AddCommand("scene", "Changes the scene", new [] {EConsoleCommandParameter.String}, args =>
			{
				var scene = (string)args[0];

				if (!SceneManager.Instance.SceneExists(scene))
				{
					AddEntry(EConsoleEntryType.Warning, "Scene not found");
					return;
				}
				
				SceneManager.Instance.ChangeScene(scene, true, true, scene != "Title");
			}, () =>
			{
				AddEntry(EConsoleEntryType.Info, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
			});
			
			AddCommand("scenes", "Lists all available scenes", () =>
			{
				AddEntry(EConsoleEntryType.Info, "Available Scenes:");

				var scenes = SceneManager.Instance.GetScenes();
				
				for (var i = 0; i < scenes.Count; i++)
					AddEntry(EConsoleEntryType.Info, scenes[i]);
			});
			
			AddCommand("noclip", "Toggle noclip mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null && !player.IsAlive)
					return;
				
				switch (player.MovementType)
				{
					case EMovementType.Normal:
						player.SetMovementType(EMovementType.Noclip);
						AddEntry(EConsoleEntryType.Info, "Enabled noclip mode");
						break;
					case EMovementType.Noclip:
						player.SetMovementType(EMovementType.Normal);
						AddEntry(EConsoleEntryType.Info, "Disabled noclip mode");
						break;
				}
			});
			
			AddCommand("god", "Toggle god mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null && !player.IsAlive)
					return;

				if (!player.IsInvulnerable)
				{
					player.SetInvulnerable(true);
					AddEntry(EConsoleEntryType.Info, "Enabled god mode");
				}
				else
				{
					player.SetInvulnerable(false);
					AddEntry(EConsoleEntryType.Info, "Disabled god mode");
				}
			});
			
			AddCommand("power", "Toggle power mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null && !player.IsAlive)
					return;

				if (!player.IsPowerful)
				{
					player.SetPowerful(true);
					AddEntry(EConsoleEntryType.Info, "Enabled power mode");
				}
				else
				{
					player.SetPowerful(false);
					AddEntry(EConsoleEntryType.Info, "Disabled power mode");
				}
			});
			
			AddCommand("timescale", "Sets the time scale", new [] {EConsoleCommandParameter.Float}, args =>
			{
				GameManager.TimeScale = (float)args[0];
			}, () =>
			{
				AddEntry(EConsoleEntryType.Info, GameManager.TimeScale.ToString(CultureInfo.CurrentCulture));
			});

			AddCommand("fps", "Sets the target framerate", new [] {EConsoleCommandParameter.Int}, args =>
			{
				GameManager.TargetFPS = (int)args[0];
			}, () =>
			{
				AddEntry(EConsoleEntryType.Info, GameManager.TargetFPS.ToString());
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

			public UnityAction<object[]> ArgumentsAction { get; private set; }

			public UnityAction BasicAction { get; private set; }

			public SConsoleCommand(string name, string description, EConsoleCommandParameter[] parameters, UnityAction<object[]> argumentsAction, UnityAction basicAction)
			{
				Name = name;
				Description = description;
				Parameters = parameters;
				ArgumentsAction = argumentsAction;
				BasicAction = basicAction;
			}
		}

		public enum EConsoleCommandParameter
		{
			String,
			Float,
			Int,
			Bool
		}

		public enum EConsoleCommandResult
		{
			NotFound,
			Success,
			IncorrectUsage
		}
	}
}