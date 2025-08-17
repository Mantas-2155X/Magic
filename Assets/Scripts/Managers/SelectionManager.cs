#define SELECTION_REQUIRE_GAMEPAD

using Managers.Events;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Managers
{
	public class SelectionManager : MonoBehaviour
	{
		private static SelectionManager instance;
		public static SelectionManager Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var copy = Addressables.InstantiateAsync("Assets/Prefabs/UI/Selection UI.prefab").WaitForCompletion();
				DontDestroyOnLoad(copy);
				
				var go = new GameObject("SelectionManager");
				DontDestroyOnLoad(go);

				instance = go.AddComponent<SelectionManager>();
				
				instance.IndicatorImage = copy.GetComponentInChildren<Image>(true);
				instance.IndicatorRect = instance.IndicatorImage.rectTransform;
				instance.Indicator = instance.IndicatorRect.gameObject;
				
				InputSystem.onDeviceChange += instance.onDeviceChange;

				instance.updateIndicatorImage();
				return instance;
			}
		}

		private static bool showIndicator;
		public static bool ShowIndicator
		{
			get => showIndicator;
			set
			{
				showIndicator = value;
				if (instance != null)
					instance.updateIndicatorImage();
			}
		}

		private EventSystem eventSystem;
		public EventSystem EventSystem
		{
			get
			{
				if (eventSystem != null)
					return eventSystem;
				
				eventSystem = EventSystem.current;
				return eventSystem;
			}
		}

		private GameObject selection;
		public GameObject Selection
		{
			get => selection;
			private set
			{
				var previousSelection = selection;
				selection = value;

				if (selection == null || !selection.TryGetComponent<RectTransform>(out var rect))
					SelectionRect = null;
				else
					SelectionRect = rect;

				SelectionIsBlocker = selection != null && selection.name == "Blocker";
				
				OnSelectionChangedEvent.Invoke(previousSelection, selection);
			}
		}

		public RectTransform SelectionRect { get; private set; }
		public bool SelectionIsBlocker { get; private set; }
		
		public GameObject Indicator { get; private set; }
		public Image IndicatorImage { get; private set; }
		public RectTransform IndicatorRect { get; private set; }
		
		public static readonly OnSelectionChangedEvent OnSelectionChangedEvent = new ();

		public void Update()
		{
			var system = EventSystem;
			if (system == null)
				return;
			
			var selected = system.currentSelectedGameObject;
			if (selected != Selection)
				Selection = selected;
			
			if (SelectionRect != null && Selection.activeSelf && !SelectionIsBlocker)
			{
				Indicator.SetActive(true);
				
				var sel = SelectionRect;
				var selRect = sel.rect;
				
				var ind = IndicatorRect;
				ind.pivot = sel.pivot;
				ind.position = sel.position;
				
				ind.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, selRect.width);
				ind.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, selRect.height);
			}
			else
			{
				Indicator.SetActive(false);
			}
		}

		public void OnDestroy()
		{
			InputSystem.onDeviceChange -= onDeviceChange;
		}

		public void SetSelection(GameObject go)
		{
			var system = EventSystem;
			if (system == null)
				return;

			system.SetSelectedGameObject(go);
			Selection = go;
		}

		private void onDeviceChange(InputDevice device, InputDeviceChange change)
		{
			if (device is not Gamepad and not Joystick)
				return;

			var title = Title.WeakInstance;
			if (title != null && !title.isActiveAndEnabled)
				title.Open();
					
			updateIndicatorImage();
		}

		private void updateIndicatorImage()
		{
#if !SELECTION_REQUIRE_GAMEPAD
			IndicatorImage.color = Color.white;
#else
			IndicatorImage.color = Gamepad.all.Count > 0 && ShowIndicator ? Color.white : Color.clear;
#endif
		}
	}
}