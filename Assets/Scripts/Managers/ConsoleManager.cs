using System;
using System.Collections.Generic;
using System.Globalization;
using AI.Enums;
using Managers.Events;
using ScriptableObjects;
using Tools;
using UI;
using UI.Settings.Pages;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

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
				Application.logMessageReceived += instance.logReceived;
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
			
			var split = new List<string>(TextTools.ParseText(name, ' ', '"'));
			for (var i = split.Count - 1; i >= 0; i--)
			{
				var entry = split[i];
				
				// Remove empty or space characters from the parameters
				if (string.IsNullOrWhiteSpace(entry))
					split.RemoveAt(i);
			}
			
			if (split.Count == 0)
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
					if (split.Count > 1)
					{
						// No parameters in command but there are some in the input, fail
						return EConsoleCommandResult.TooManyParameters;
					}
					
					// No parameters in command and input, run the basic action
					command.BasicAction();
					return EConsoleCommandResult.Success;
				}
				
				if (commandParameters.Length != split.Count - 1)
				{
					if (split.Count - 1 != 0 || command.BasicAction == null)
					{
						// Match parameter count if there's no basic action
						return commandParameters.Length > split.Count - 1 ? EConsoleCommandResult.NotEnoughParameters : EConsoleCommandResult.TooManyParameters;
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
								return EConsoleCommandResult.InvalidParameter;
							}
							inputParameters[k] = floatValue;
							break;
						case EConsoleCommandParameter.Int:
							if (!int.TryParse(split[k + 1], NumberStyles.Integer, CultureInfo.CurrentCulture, out var intValue))
							{
								// Command parameter should be an int but the input parameter isn't, fail
								return EConsoleCommandResult.InvalidParameter;
							}
							inputParameters[k] = intValue;
							break;
						case EConsoleCommandParameter.Bool:
							if (!bool.TryParse(split[k + 1], out var boolValue))
							{
								// Command parameter should be a bool but the input parameter isn't, fail
								return EConsoleCommandResult.InvalidParameter;
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
				SceneManager.Instance.QuitGame();
			});
			
			AddCommand("title", "Return to title", () =>
			{
				SceneManager.Instance.ChangeScene(ObjectManager.Instance.GetData<SceneData>("SCENE_TITLE_NAME"), true, true, false);
			});
			
			AddCommand("log", "Create test logs", new [] {EConsoleCommandParameter.String, EConsoleCommandParameter.Int}, args =>
			{
				var amount = (int)args[1];
				if (amount <= 0)
				{
					Debug.LogWarning("Invalid amount specified");
					return;
				}
				
				var type = (string)args[0];
				switch (type)
				{
					case "error":
					{
						for (var i = 0; i < amount; i++) 
							Debug.LogError("test");
						break;
					}
					case "assert":
					{
						for (var i = 0; i < amount; i++) 
							Debug.LogAssertion("test");
						break;
					}
					case "exception":
					{
						for (var i = 0; i < amount; i++) 
							Debug.LogException(new Exception("test"));
						break;
					}
					case "warning":
					{
						for (var i = 0; i < amount; i++) 
							Debug.LogWarning("test");
						break;
					}
					case "log":
					{
						for (var i = 0; i < amount; i++) 
							Debug.Log("test");
						break;
					}
					default:
						Debug.LogWarning("Incorrect type specified (error, assert, exception, warning, log)");
						return;
				}
			});
			
			AddCommand("scene", "Changes the scene", new [] {EConsoleCommandParameter.String}, args =>
			{
				var sceneData = ObjectManager.Instance.GetData<SceneData>((string)args[0]);

				if (!SceneManager.Instance.SceneExists(sceneData) || sceneData == null)
				{
					Debug.LogWarning("Scene not found");
					return;
				}
				
				if (sceneData.Internal)
				{
					Debug.LogWarning("Internal scenes can not be manually loaded");
					return;
				}
				
				SceneManager.Instance.ChangeScene(sceneData, true, true, sceneData.Name != "SCENE_TITLE_NAME");
			}, () =>
			{
				Debug.Log(SceneManager.Instance.GetCurrentSceneData().Name);
			});
			
			AddCommand("scenes", "Lists all available scenes", () =>
			{
				Debug.Log("Available Scenes:");

				var datas = ObjectManager.Instance.GetAllDatas<SceneData>();
				
				for (var i = 0; i < datas.Count; i++)
					Debug.Log(datas[i].Name);
			});
			
			AddCommand("noclip", "Toggle noclip mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;
				
				switch (player.MovementType)
				{
					case EMovementType.Normal:
						player.SetMovementType(EMovementType.Noclip);
						break;
					case EMovementType.Noclip:
						player.SetMovementType(EMovementType.Normal);
						break;
				}
			});
			
			AddCommand("god", "Toggle god mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;

				player.SetInvulnerable(!player.IsInvulnerable);
			});
			
			AddCommand("power", "Toggle power mode", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;

				player.SetPowerful(!player.IsPowerful);
			});
			
			AddCommand("learn", "Give the player a spell", new [] {EConsoleCommandParameter.String}, args =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;

				var spell = "";

				for (var i = 0; i < args.Length; i++)
					spell += (string)args[i];
				
				player.LearnSpell(ObjectManager.Instance.GetData<SpellData>(spell), false);
			});
			
			AddCommand("give", "Give the player a wearable", new [] {EConsoleCommandParameter.String}, args =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;

				var wearable = "";

				for (var i = 0; i < args.Length; i++)
					wearable += (string)args[i];
				
				player.EquipWearable(ObjectManager.Instance.GetData<WearableData>(wearable));
			});
			
			AddCommand("object", "Creates an object where the player is looking", new [] {EConsoleCommandParameter.String}, args =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
					return;

				var objName = "";

				for (var i = 0; i < args.Length; i++)
					objName += (string)args[i];

				var cam = Camera.main;
				if (cam == null)
					return;
				
				if (!Physics.Raycast(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, float.MaxValue, ~LayerMaskTools.GetMaskWithAlives()))
					return;
				
				ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetData<ObjectData>(objName), hit.point + Vector3.up, new Vector3(0, player.transform.eulerAngles.y, 0));
			});
			
			AddCommand("objects", "Lists all available objects", () =>
			{
				Debug.Log("Available Objects:");

				var datas = ObjectManager.Instance.GetAllDatas<ObjectData>();
				
				for (var i = 0; i < datas.Count; i++)
					Debug.Log(datas[i].Name);
			});
			
			AddCommand("timescale", "Sets the time scale", new [] {EConsoleCommandParameter.Float}, args =>
			{
				GameManager.TimeScale = (float)args[0];
			}, () =>
			{
				Debug.Log(GameManager.TimeScale.ToString(CultureInfo.CurrentCulture));
			});

			AddCommand("clear", "Clears the console", () =>
			{
				ClearEntries();
			});
			
			AddCommand("kill", "Kills the player", () =>
			{
				var player = AIManager.Instance.Player;
				if (player == null || !player.IsAlive)
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
				Debug.Log("Available Commands:");
				
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
					
					Debug.Log($"{command.Name} {parameters}- {command.Description}");
				}
			});
		}

		#endregion
		
		private void logReceived(string logString, string stackTrace, LogType type)
		{
			EConsoleEntryType entryType;

			switch (type)
			{
				case LogType.Error:
				case LogType.Assert:
				case LogType.Exception:
					entryType = EConsoleEntryType.Error;
					break;
				case LogType.Warning:
					entryType = EConsoleEntryType.Warning;
					break;
				case LogType.Log:
					entryType = EConsoleEntryType.Info;
					break;
				default:
					throw new NotImplementedException();
			}
			
			var logSplit = logString.Split('\n');

			for (var i = 0; i < logSplit.Length; i++)
			{
				var str = logSplit[i];
				
				if (string.IsNullOrWhiteSpace(str))
					continue;
				
				AddEntry(entryType, str);
			}

			if (entryType == EConsoleEntryType.Error)
			{
				var stackTraceSplit = stackTrace.Split('\n');

				for (var i = 0; i < stackTraceSplit.Length; i++)
				{
					var str = stackTraceSplit[i];
				
					if (string.IsNullOrWhiteSpace(str))
						continue;
				
					AddEntry(entryType, str);
				}
			}
		}
		
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
			InvalidParameter,
			TooManyParameters,
			NotEnoughParameters
		}
	}
}