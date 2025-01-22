using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Cysharp.Threading.Tasks;
using Managers;
using TMPro;
using UnityEngine;
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
		public Scrollbar Scrollbar;
		
		[SerializeField]
		public TMP_InputField Input;
		
		[SerializeField]
		public List<TMP_Text> Items = new ();

		[SerializeField]
		public SerializedDictionary<ConsoleManager.EConsoleEntryType, Color> Colors;
		
		private bool entriesChanged = true;
		
		public void Awake()
		{
			ConsoleManager.OnConsoleEntryAddedEvent.AddListener(onConsoleEntryAdded);
			ConsoleManager.OnConsoleClearedEvent.AddListener(onConsoleCleared);
		}

		public void OnEnable()
		{
			if (entriesChanged)
				refresh();
			
			selectDelayed().Forget();
		}
		
		public void OnCloseClicked()
		{
			Display(false);
		}
		
		public void OnSubmitClicked()
		{
			if (string.IsNullOrEmpty(Input.text))
				return;
			
			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, $">{Input.text}");

			try
			{
				var result = ConsoleManager.Instance.ExecuteCommand(Input.text);
				switch (result)
				{
					case ConsoleManager.EConsoleCommandResult.NotFound:
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Command not found");
						break;
					case ConsoleManager.EConsoleCommandResult.Success:
						// all good
						break;
					case ConsoleManager.EConsoleCommandResult.IncorrectUsage:
						ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Warning, "Incorrect usage");
						break;
					default:
						throw new NotImplementedException();
				}
			}
			catch (Exception e)
			{
				ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Error, "Failed executing command");
				UnityEngine.Debug.LogWarning($"[Console] Failed executing command {Input.text}, {e}");
			}
			
			Input.SetTextWithoutNotify("");
			
			Input.Select();
			Input.ActivateInputField();
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
			entriesChanged = false;

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
		}
		
		private void onConsoleEntryAdded(ConsoleManager.SConsoleEntry entry)
		{
			if (!isActiveAndEnabled)
			{
				entriesChanged = true;
				return;
			}
			
			refresh();
		}
		
		private void onConsoleCleared()
		{
			if (!isActiveAndEnabled)
			{
				entriesChanged = true;
				return;
			}
			
			refresh();
		}

		private async UniTaskVoid selectDelayed()
		{
			await UniTask.NextFrame();
			
			if (!isActiveAndEnabled || Input == null)
				return;
			
			Scrollbar.value = 1f;

			Input.Select();
			Input.ActivateInputField();
		}
	}
}