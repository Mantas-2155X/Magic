using System;
using System.Collections.Generic;
using System.IO;
using AYellowpaper.SerializedCollections;
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
		public Transform Template;

		[SerializeField]
		public Transform Content;

		[SerializeField]
		public ScrollRect ScrollRect;
		
		[SerializeField]
		public TMP_InputField Input;
		
		[SerializeField]
		public InputActionReference HistoryAction;

		[SerializeField]
		public List<TMP_Text> Items = new ();

		[SerializeField]
		public SerializedDictionary<ConsoleManager.EConsoleEntryType, Color> Colors;
		
		private readonly List<string> history = new ();
		private int historyIndex = -1;
		
		public void Awake()
		{
			ConsoleManager.OnConsoleEntryAddedEvent.AddListener(onConsoleEntryAdded);
			ConsoleManager.OnConsoleClearedEvent.AddListener(onConsoleCleared);
			
			var historyAction = HistoryAction.action;
			historyAction.performed += onHistory;
			historyAction.Enable();
			
			Input.onSubmit.AddListener(onSubmit);
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
			selectDelayed().Forget();
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
			
			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, $">{text}");

			try
			{
				var result = ConsoleManager.Instance.ExecuteCommand(text);
				switch (result)
				{
					case ConsoleManager.EConsoleCommandResult.NotFound:
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Command not found");
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
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Invalid parameters");
						break;
					case ConsoleManager.EConsoleCommandResult.TooManyParameters:
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Too many parameters");
						break;
					case ConsoleManager.EConsoleCommandResult.NotEnoughParameters:
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Not enough parameters");
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
			
			selectDelayed().Forget();
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
		}
		
		private void refresh()
		{
			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];
				item.gameObject.SetActive(false);
			}
			
			var entries = ConsoleManager.Instance.GetEntries();
			for (var i = 0; i < entries.Count; i++)
			{
				TMP_Text item;
				
				if (Items.Count <= i)
				{
					item = Instantiate(Template, Content).GetComponent<TMP_Text>();
					Items.Add(item);
				}
				else
				{
					item = Items[i];
				}
				
				var entry = entries[i];
				
				item.color = Colors[entry.Type];
				item.text = entry.Text;
				
				item.gameObject.SetActive(true);
			}
			
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
			if (!isActiveAndEnabled)
				return;
			
			refresh();
		}
		
		private void onConsoleCleared()
		{
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
			
			Input.Select();
			Input.ActivateInputField();
		}

		private async UniTaskVoid scrollDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;

			Canvas.ForceUpdateCanvases();
			
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