using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
	public class Console : MonoBehaviour
	{
		[SerializeField]
		public ScrollRect ScrollRect;
		
		[SerializeField]
		public TMP_InputField Input;

		[SerializeField]
		public TMP_InputField Text;
		
		[SerializeField]
		public InputActionReference HistoryAction;

		private const string whiteColor = "<color=white>";
		private const string yellowColor = "<color=yellow>";
		private const string redColor = "<color=red>";
		private const string endColor = "</color>";

		private readonly string newLine = Environment.NewLine;
		
		private readonly List<string> history = new ();
		private int historyIndex = -1;

		private bool refreshEverything;
		private int refreshLast;
		
		public void Awake()
		{
			ConsoleManager.OnConsoleEntryAddedEvent.AddListener(onConsoleEntryAdded);
			ConsoleManager.OnConsoleClearedEvent.AddListener(onConsoleCleared);
			
			var historyAction = HistoryAction.action;
			historyAction.performed += onHistory;
			historyAction.Enable();
			
			Input.onSubmit.AddListener(onSubmit);

			refreshEverything = true;
		}
		
		public void OnDestroy()
		{
			ConsoleManager.OnConsoleEntryAddedEvent.RemoveListener(onConsoleEntryAdded);
			ConsoleManager.OnConsoleClearedEvent.RemoveListener(onConsoleCleared);
		}

		public void OnEnable()
		{
			transform.SetAsLastSibling();
			historyIndex = history.Count;
			refresh();
			Select();
		}

		public void OnOpenLogsClicked()
		{
			var filePath = Application.consoleLogPath;
			if (string.IsNullOrEmpty(filePath))
			{
				UnityEngine.Debug.LogWarning("[Console] Log path not found");
				return;
			}

			var fileInfo = new FileInfo(filePath);
			if (!fileInfo.Exists)
			{
				UnityEngine.Debug.LogWarning("[Console] Log file not found");
				return;
			}

			var directory = fileInfo.DirectoryName;
			if (string.IsNullOrEmpty(directory))
			{
				UnityEngine.Debug.LogWarning("[Console] Log directory not found");
				return;
			}

			UnityEngine.Debug.Log($"[Console] Opening path {directory}");
			Application.OpenURL($"file:///{directory}");
		}
		
		public void OnCloseClicked()
		{
			Display(false);
		}
		
		public void OnSubmitClicked()
		{
			var text = Input.text;
			
			if (string.IsNullOrEmpty(text))
				return;
			
			UnityEngine.Debug.Log($">{text}");

			try
			{
				var result = ConsoleManager.Instance.ExecuteCommand(text);
				switch (result)
				{
					case ConsoleManager.EConsoleCommandResult.NotFound:
						UnityEngine.Debug.LogWarning("Command not found");
						break;
					case ConsoleManager.EConsoleCommandResult.Success:
						if (history.Count > 0)
						{
							var lastEntry = history[^1];
							if (lastEntry != text)
							{
								history.Add(text);
								historyIndex = history.Count;
							}
						}
						else
						{
							history.Add(text);
							historyIndex = history.Count;
						}
						break;
					case ConsoleManager.EConsoleCommandResult.InvalidParameter:
						UnityEngine.Debug.LogWarning("Invalid parameters");
						break;
					case ConsoleManager.EConsoleCommandResult.TooManyParameters:
						UnityEngine.Debug.LogWarning("Too many parameters");
						break;
					case ConsoleManager.EConsoleCommandResult.NotEnoughParameters:
						UnityEngine.Debug.LogWarning("Not enough parameters");
						break;
					default:
						throw new NotImplementedException();
				}
			}
			catch (Exception e)
			{
				UnityEngine.Debug.LogWarning($"[Console] Failed executing command {text}, {e}");
			}
			
			Input.SetTextWithoutNotify("");
			Select();
		}

		public void Toggle()
		{
			Display(!isActiveAndEnabled);
		}
		
		public void Display(bool state)
		{
			if (state == isActiveAndEnabled)
				return;
			
			if (state)
			{
				var title = Title.Instance;
				if (title != null && !title.isActiveAndEnabled)
					title.Open();
			}

			gameObject.SetActive(state);
			
			if (state)
				Select();
			else
				Title.Instance.Select(true);
		}
		
		public void Select()
		{
			selectDelayed().Forget();
		}
		
		private void refresh()
		{
			var entries = ConsoleManager.Instance.GetEntries();
			var startAt = 0;

			if (refreshEverything)
			{
				Text.text = "";
			}
			else if (refreshLast == 0)
			{
				refreshEverything = false;
				refreshLast = 0;
				return;
			}
			else if (refreshLast != 0)
			{
				startAt = entries.Count - refreshLast;
			}

			refreshEverything = false;
			refreshLast = 0;

			if (entries.Count == 0 || startAt < 0 || startAt >= entries.Count)
				return;
			
			var builder = new StringBuilder(Text.text);

			for (var i = startAt; i < entries.Count; i++)
			{
				var entry = entries[i];
				
				switch (entry.Type)
				{
					case ConsoleManager.EConsoleEntryType.Info:
						builder.Append(whiteColor);
						break;
					case ConsoleManager.EConsoleEntryType.Warning:
						builder.Append(yellowColor);
						break;
					case ConsoleManager.EConsoleEntryType.Error:
						builder.Append(redColor);
						break;
					default:
						throw new NotImplementedException();
				}
				
				builder.Append(entry.Text);
				builder.Append(endColor);
				builder.Append(newLine);
			}
			
			Text.text = builder.ToString();
			scrollDelayed().Forget();
		}

		private void onSubmit(string text)
		{
			if (Input.wasCanceled)
				return;
			
			OnSubmitClicked();
		}
		
		private void onConsoleEntryAdded(ConsoleManager.SConsoleEntry entry)
		{
			refreshLast++;

			if (!isActiveAndEnabled)
				return;
			
			refresh();
		}
		
		private void onConsoleCleared()
		{
			refreshEverything = true;
			
			if (!isActiveAndEnabled)
				return;
			
			refresh();
		}

		private void onHistory(InputAction.CallbackContext ctx)
		{
			var value = ctx.ReadValue<Vector2>();
			switch (value.y)
			{
				case > 0f:
					previousHistory();
					break;
				case < 0f:
					nextHistory();
					break;
			}
		}
		
		private void previousHistory()
		{
			if (!Input.IsActive() || !Input.isFocused)
				return;

			var historySize = history.Count;
			if (historySize == 0)
				return;

			historyIndex--;

			// Loop around if reached the end
			if (historyIndex < 0)
				historyIndex = historySize - 1;
			
			Input.SetTextWithoutNotify(history[historyIndex]);
			moveToEndDelayed().Forget();
		}
		
		private void nextHistory()
		{
			if (!Input.IsActive() || !Input.isFocused)
				return;

			var historySize = history.Count;
			if (historySize == 0)
				return;

			historyIndex++;

			// Loop around if reached the end
			if (historyIndex >= historySize)
				historyIndex = 0;
			
			Input.SetTextWithoutNotify(history[historyIndex]);
			moveToEndDelayed().Forget();
		}
		
		private async UniTaskVoid selectDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			Input.ActivateInputField();
			Input.Select();
		}

		private async UniTaskVoid scrollDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;

			LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)ScrollRect.transform);
			
			ScrollRect.verticalNormalizedPosition = 0f;
			ScrollRect.horizontalNormalizedPosition = 0f;
		}
		
		private async UniTaskVoid moveToEndDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;

			Input.MoveToEndOfLine(false, false);
		}
	}
}