using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using State;
using UI.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SceneManager = Managers.SceneManager;

namespace UI
{
	public class SaveLoad : MonoBehaviour
	{
		[SerializeField]
		public Transform Template;

		[SerializeField]
		public GridLayoutGroup GridLayoutGroup;
		
		[SerializeField]
		public Button CloseButton;
		
		[SerializeField]
		public Button SaveButton;
		
		[SerializeField]
		public Button LoadButton;
		
		[SerializeField]
		public Button DeleteButton;

		[SerializeField]
		public ScrollRect ScrollRect;

		[NonSerialized]
		public readonly List<SceneContainer> Containers = new ();

		private SceneContainer selectedContainer;
		private Tuple<string, PartialSaveData> selectedSave;
		
		private int activeSaves;

		public void OnEnable()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneChanged;

			transform.SetAsLastSibling();
			
			updateSaves();
			updateNavigation();
			updateSaveButton();
			Select();
		}

		public void OnDisable()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onSceneChanged;
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
				if (!title.isActiveAndEnabled)
					title.Open();
			}
			
			gameObject.SetActive(state);

			if (state)
			{
				updateSaves();
				updateNavigation();
				updateSaveButton();
				Select();
			}
			else
			{
				Title.Instance.Select(true);
			}
		}
		
		public void OnCloseClicked()
		{
			Display(false);
		}

		public void OnSaveClicked()
		{
			if (selectedContainer == null)
			{
				StateManager.Instance.Save(out _);
						
				updateSaves();
				updateNavigation();
				Select();
				
				return;
			}
			
			Title.Instance.Confirm.Show(EConfirmPreset.OverwriteSave, result =>
			{
				if (!result)
					return;

				if (selectedContainer != null)
					StateManager.Instance.Delete(selectedSave.Item1);
				
				StateManager.Instance.Save(out _);
						
				updateSaves();
				updateNavigation();
				Select();
			});
		}

		public void OnLoadClicked()
		{
			if (SceneManager.Instance.IsInTitle())
			{
				if (selectedContainer == null)
					return;

				StateManager.Instance.Load(selectedSave.Item2);
				
				return;
			}
			
			Title.Instance.Confirm.Show(EConfirmPreset.LoadSave, result =>
			{
				if (!result)
					return;
				
				if (selectedContainer == null)
					return;

				StateManager.Instance.Load(selectedSave.Item2);
			});
		}

		public void OnDeleteClicked()
		{
			Title.Instance.Confirm.Show(EConfirmPreset.DeleteSave, result =>
			{
				if (!result)
					return;
				
				if (selectedContainer == null)
					return;
			
				StateManager.Instance.Delete(selectedSave.Item1);
			
				updateSaves();
				updateNavigation();
				Select();
			});
		}

		public void Select()
		{
			selectDelayed().Forget();
		}
		
		private async UniTaskVoid selectDelayed()
		{
			if (activeSaves == 0)
			{
				SelectionManager.Instance.SetSelection(SaveButton.gameObject);
				return;
			}
			
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			SelectionManager.Instance.SetSelection(Containers[0].Button.gameObject);
			Containers[0].OnSelect(new AxisEventData(null));
		}

		private void onContainerClicked(SceneContainer container)
		{
			if (selectedContainer != null)
				selectedContainer.Image.color = Color.white;

			selectedContainer = container == selectedContainer ? null : container;

			if (selectedContainer != null)
			{
				selectedContainer.Image.color = new Color(0.85f, 0.85f, 0.85f);
				
				LoadButton.interactable = true;
				LoadButton.image.color = Color.black;

				DeleteButton.interactable = true;
				DeleteButton.image.color = Color.black;
			}
			else
			{
				LoadButton.interactable = false;
				LoadButton.image.color = Color.gray;

				DeleteButton.interactable = false;
				DeleteButton.image.color = Color.gray;
			}
		}
		
		private void updateSaves()
		{
			var isInTitle = SceneManager.Instance.IsInTitle();
			var currentSceneData = SceneManager.Instance.GetCurrentSceneData();

			var allSaves = StateManager.Instance.GetSaves();
			var availableSaves = new List<Tuple<string, PartialSaveData>>();

			foreach (var pair in allSaves)
			{
				// Show all saves in title, otherwise only show current scene saves
				if (currentSceneData.Name != pair.Value.Scene && !isInTitle)
					continue;
			
				availableSaves.Add(new Tuple<string, PartialSaveData>(pair.Key, pair.Value));
			}

			// Sort by saved time descending
			availableSaves.Sort((x, y) => y.Item2.SavedTime.CompareTo(x.Item2.SavedTime));
			
			var savesCount = availableSaves.Count;
			if (savesCount > Containers.Count)
			{
				var toCreate = savesCount - Containers.Count;
				if (toCreate > 0)
				{
					var parent = Template.parent;
					
					for (var i = 0; i < toCreate; i++)
					{
						var copy = Instantiate(Template.gameObject, parent);
						copy.name = $"Container {Containers.Count}";
				
						Containers.Add(copy.GetComponent<SceneContainer>());
					}
				}
			}
			
			activeSaves = 0;
			onContainerClicked(null);

			var containerIndex = 0;
			for (var i = 0; i < availableSaves.Count; i++)
			{
				var (savePath, saveData) = availableSaves[i];

				var sceneData = ObjectManager.Instance.GetData<SceneData>(saveData.Scene);
				var container = Containers[containerIndex];

				container.Button.onClick.RemoveAllListeners();
				container.Button.onClick.AddListener(delegate
				{
					selectedSave = new Tuple<string, PartialSaveData>(savePath, saveData);
					onContainerClicked(container);
				});

				container.Image.sprite = sceneData.Icon;

				container.Localizer.Key = sceneData.Name;
				container.Localizer.Apply();

				container.Date.text = saveData.SavedTime.ToString("yyyy-MM-dd\nHH:mm:ss");

				container.AutoSave.SetActive(saveData.AutoSave);
				
				container.Button.gameObject.SetActive(true);

				containerIndex++;
				activeSaves++;
			}

			if (containerIndex != Containers.Count)
			{
				for (var i = containerIndex; i < Containers.Count; i++)
				{
					Containers[i].Button.gameObject.SetActive(false);
				}
			}

			GridLayoutGroup.cellSize = activeSaves <= 6 ? new Vector2(219, 164) : new Vector2(215, 160);
			
			if (isActiveAndEnabled)
				updateNavigation();
		}
		
		private void updateNavigation()
		{
			var savesCount = activeSaves;
			if (savesCount == 0)
			{
				CloseButton.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnDown = LoadButton,
					selectOnUp = LoadButton,
					selectOnRight = DeleteButton,
					selectOnLeft = LoadButton
				};
			
				DeleteButton.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnDown = CloseButton,
					selectOnUp = CloseButton,
					selectOnRight = SaveButton,
					selectOnLeft = CloseButton
				};
			
				SaveButton.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnDown = CloseButton,
					selectOnUp = CloseButton,
					selectOnRight = LoadButton,
					selectOnLeft = DeleteButton
				};
			
				LoadButton.navigation = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnDown = CloseButton,
					selectOnUp = CloseButton,
					selectOnRight = CloseButton,
					selectOnLeft = SaveButton
				};
				return;
			}

			var firstContainer = Containers[0].Button;
			var lastContainer = Containers[savesCount - 1].Button;

			var constraints = GridLayoutGroup.constraintCount;
			
			CloseButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = firstContainer,
				selectOnUp = LoadButton,
				selectOnRight = DeleteButton,
				selectOnLeft = LoadButton
			};
			
			DeleteButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = CloseButton,
				selectOnUp = lastContainer,
				selectOnRight = SaveButton,
				selectOnLeft = CloseButton
			};
			
			SaveButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = CloseButton,
				selectOnUp = lastContainer,
				selectOnRight = LoadButton,
				selectOnLeft = DeleteButton
			};
			
			LoadButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = CloseButton,
				selectOnUp = lastContainer,
				selectOnRight = CloseButton,
				selectOnLeft = SaveButton
			};

			for (var i = 0; i < savesCount; i++)
			{
				var container = Containers[i].Button;

				var nav = new Navigation
				{
					mode = Navigation.Mode.Explicit
				};

				Button previousContainer;
				Button nextContainer;

				if (i == 0)
				{
					previousContainer = lastContainer;
					nextContainer = savesCount - 1 > 1 ? Containers[i + 1].Button : container;
				}
				else if (i == savesCount - 1)
				{
					previousContainer = savesCount - 1 > 1 ? Containers[i - 1].Button : container;
					nextContainer = firstContainer;
				}
				else
				{
					previousContainer = Containers[i - 1].Button;
					nextContainer = Containers[i + 1].Button;
				}
				
				if (container == firstContainer)
				{
					nav.selectOnLeft = lastContainer;
					nav.selectOnRight = nextContainer;
				}
				else if (container == lastContainer)
				{
					nav.selectOnLeft = previousContainer;
					nav.selectOnRight = firstContainer;
				}
				else
				{
					nav.selectOnLeft = previousContainer;
					nav.selectOnRight = nextContainer;
				}

				Button aboveButton;
				Button belowButton;

				var aboveIndex = i - constraints;
				if (aboveIndex < 0)
				{
					aboveButton = CloseButton;
				}
				else
				{
					aboveButton = Containers[aboveIndex].Button;
				}
				
				var belowIndex = i + constraints;
				if (belowIndex >= savesCount)
				{
					belowButton = LoadButton;
				}
				else
				{
					belowButton = Containers[belowIndex].Button;
				}

				nav.selectOnUp = aboveButton;
				nav.selectOnDown = belowButton;
				
				container.navigation = nav;
			}
		}

		private void updateSaveButton()
		{
			var currentSceneData = SceneManager.Instance.GetCurrentSceneData();
			if (currentSceneData != null && currentSceneData.SupportsSaving)
			{
				SaveButton.interactable = true;
				SaveButton.image.color = Color.black;
			}
			else
			{
				SaveButton.interactable = false;
				SaveButton.image.color = Color.gray;
			}
		}
		
		private void onSceneChanged(Scene scene, LoadSceneMode mode)
		{
			if (!isActiveAndEnabled)
				return;
			
			updateSaveButton();
		}
	}
}