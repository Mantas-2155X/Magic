using Managers.Events;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

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

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Selection UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					Debug.LogError("[SelectionManager] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);
				
				var go = new GameObject("SelectionManager");
				DontDestroyOnLoad(go);

				instance = go.AddComponent<SelectionManager>();
				
				instance.IndicatorImage = copy.GetComponentInChildren<Image>(true);
				instance.IndicatorRect = instance.IndicatorImage.rectTransform;
				instance.Indicator = instance.IndicatorRect.gameObject;
				
				InputSystem.onDeviceChange += instance.onDeviceChange;

				if (Gamepad.all.Count > 0)
				{
					ControllerConnected = true;
					instance.IndicatorImage.color = Color.white;
				}
				
				return instance;
			}
		}

		public static readonly OnSelectionChangedEvent OnSelectionChangedEvent = new ();
		
		public static bool ControllerConnected { get; private set; }

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
			set
			{
				var previousSelection = selection;
				selection = value;

				if (selection == null || !selection.TryGetComponent<RectTransform>(out var rect))
					SelectionRect = null;
				else
					SelectionRect = rect;
				
				OnSelectionChangedEvent.Invoke(previousSelection, selection);
			}
		}

		public RectTransform SelectionRect { get; private set; }
		
		public GameObject Indicator { get; private set; }
		public Image IndicatorImage { get; private set; }
		public RectTransform IndicatorRect { get; private set; }
		
		public void Update()
		{
			var system = EventSystem;
			if (system == null)
				return;
			
			var selected = system.currentSelectedGameObject;
			if (selected != Selection)
				Selection = selected;
			
			if (SelectionRect != null && Selection.activeSelf && Selection.name != "Blocker")
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

			switch (change)
			{
				case InputDeviceChange.Added or InputDeviceChange.Enabled or InputDeviceChange.Reconnected:
					ControllerConnected = true;
					IndicatorImage.color = Color.white;

					if (title != null && !title.isActiveAndEnabled)
						title.Open();
					
					break;
				case InputDeviceChange.Removed or InputDeviceChange.Disabled or InputDeviceChange.Disconnected:
					ControllerConnected = false;
					IndicatorImage.color = Color.clear;
					
					if (title != null && !title.isActiveAndEnabled)
						title.Open();
					
					break;
			}
		}
	}
}