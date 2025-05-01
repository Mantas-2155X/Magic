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

		[NonSerialized]
		public readonly List<(Button, Image, Localizer)> Containers = new ();

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

		public void Select()
		{
			selectDelayed().Forget();
		}
		
		private async UniTaskVoid selectDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			SelectionManager.Instance.SetSelection(Containers[0].Item1.gameObject);
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
				
						Containers.Add((copy.GetComponent<Button>(), copy.GetComponent<Image>(), copy.GetComponentInChildren<Localizer>()));
					}
				}
			}
			
			for (var i = 0; i < Containers.Count; i++)
			{
				var container = Containers[i];
				
				if (i >= scenesCount)
				{
					container.Item1.gameObject.SetActive(false);
					continue;
				}

				var idx = i;

				container.Item1.onClick.RemoveAllListeners();
				container.Item1.onClick.AddListener(delegate
				{
					Display(false);
					SceneManager.Instance.ChangeScene(sceneNames[idx], true, true, true);
				});
				
				container.Item2.sprite = sceneDatas[i].Icon;
				
				container.Item3.Key = sceneDatas[i].Name;
				container.Item3.Apply();
				
				container.Item1.gameObject.SetActive(true);
			}
			
			if (isActiveAndEnabled)
				updateNavigation();
		}

		private void updateNavigation()
		{
			var scenesCount = Containers.Count;
			
			var firstContainer = Containers[0].Item1;
			var lastContainer = Containers[scenesCount - 1].Item1;

			var constraints = GridLayoutGroup.constraintCount;
			
			CloseButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = firstContainer,
				selectOnUp = lastContainer
			};

			for (var i = 0; i < scenesCount; i++)
			{
				var container = Containers[i].Item1;

				var nav = new Navigation
				{
					mode = Navigation.Mode.Explicit
				};

				Button previousContainer;
				Button nextContainer;

				if (i == 0)
				{
					previousContainer = lastContainer;
					nextContainer = scenesCount - 1 > 1 ? Containers[i + 1].Item1 : container;
				}
				else if (i == scenesCount - 1)
				{
					previousContainer = scenesCount - 1 > 1 ? Containers[i - 1].Item1 : container;
					nextContainer = firstContainer;
				}
				else
				{
					previousContainer = Containers[i - 1].Item1;
					nextContainer = Containers[i + 1].Item1;
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
					aboveButton = Containers[aboveIndex].Item1;
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
					belowButton = Containers[belowIndex].Item1;
				}

				nav.selectOnUp = aboveButton;
				nav.selectOnDown = belowButton;
				
				container.navigation = nav;
			}
		}
	}
}