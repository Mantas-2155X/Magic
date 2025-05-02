using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class SceneSelect : MonoBehaviour
	{
		[SerializeField]
		public Transform Template;

		[SerializeField]
		public GridLayoutGroup GridLayoutGroup;
		
		[SerializeField]
		public Button CloseButton;
		
		[SerializeField]
		public ScrollRect ScrollRect;

		[SerializeField]
		public Toggle ShowHiddenToggle;

		[NonSerialized]
		public readonly List<SceneContainer> Containers = new ();

		private int activeScenes;

		public void OnEnable()
		{
			transform.SetAsLastSibling();
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
			{
				updateScenes();
				updateNavigation();
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

		public void OnShowHiddenChanged(bool state)
		{
			if (!isActiveAndEnabled)
				return;
			
			updateScenes();
			updateNavigation();
			Select();
		}
		
		public void Select()
		{
			selectDelayed().Forget();
		}
		
		private async UniTaskVoid selectDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			SelectionManager.Instance.SetSelection(Containers[0].Button.gameObject);
		}
		
		public void updateScenes()
		{
			var sceneNames = SceneManager.Instance.GetSceneNames();
			var sceneDatas = SceneManager.Instance.GetSceneDatas();
			
			var scenesCount = sceneDatas.Count;
			if (scenesCount > Containers.Count)
			{
				var toCreate = scenesCount - Containers.Count;
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
			
			activeScenes = 0;

			var containerIndex = 0;
			for (var i = 0; i < sceneDatas.Count; i++)
			{
				var sceneData = sceneDatas[i];
				if (sceneData.Hidden && !ShowHiddenToggle.isOn)
					continue;

				var container = Containers[containerIndex];
				var idx = i;

				container.Button.onClick.RemoveAllListeners();
				container.Button.onClick.AddListener(delegate
				{
					Display(false);
					SceneManager.Instance.ChangeScene(sceneNames[idx], true, true, true);
				});
				
				container.Image.sprite = sceneDatas[i].Icon;
				
				container.Localizer.Key = sceneDatas[i].Name;
				container.Localizer.Apply();
				
				container.Button.gameObject.SetActive(true);
				
				containerIndex++;
				activeScenes++;
			}

			if (containerIndex != Containers.Count)
			{
				for (var i = containerIndex; i < Containers.Count; i++)
				{
					Containers[i].Button.gameObject.SetActive(false);
				}
			}

			GridLayoutGroup.cellSize = activeScenes <= 7 ? new Vector2(219, 164) : new Vector2(215, 160);
			
			if (isActiveAndEnabled)
				updateNavigation();
		}

		private void updateNavigation()
		{
			var scenesCount = activeScenes;
			
			var firstContainer = Containers[0].Button;
			var lastContainer = Containers[scenesCount - 1].Button;

			var constraints = GridLayoutGroup.constraintCount;
			
			CloseButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = firstContainer,
				selectOnUp = lastContainer,
				selectOnLeft = ShowHiddenToggle,
				selectOnRight = ShowHiddenToggle
			};
			
			ShowHiddenToggle.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = firstContainer,
				selectOnUp = lastContainer,
				selectOnLeft = CloseButton,
				selectOnRight = CloseButton
			};

			for (var i = 0; i < scenesCount; i++)
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
					nextContainer = scenesCount - 1 > 1 ? Containers[i + 1].Button : container;
				}
				else if (i == scenesCount - 1)
				{
					previousContainer = scenesCount - 1 > 1 ? Containers[i - 1].Button : container;
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
				if (belowIndex >= scenesCount)
				{
					if (aboveButton != CloseButton)
					{
						belowButton = CloseButton;
					}
					else
					{
						belowButton = lastContainer;
					}
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
	}
}