using Managers.Events;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
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

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Selection UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					Debug.LogError("[SelectionManager] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);
				
				var indicatorRect = copy.GetComponentInChildren<Image>(true).rectTransform;
				
				var go = new GameObject("SelectionManager");
				DontDestroyOnLoad(go);

				instance = go.AddComponent<SelectionManager>();
				
				instance.Indicator = indicatorRect.gameObject;
				instance.IndicatorRect = indicatorRect;
				
				return instance;
			}
		}

		public static readonly OnSelectionChangedEvent OnSelectionChangedEvent = new ();
		
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
		public RectTransform IndicatorRect { get; private set; }
		
		public void Update()
		{
			var system = EventSystem;
			if (system == null)
				return;
			
			var selected = system.currentSelectedGameObject;
			if (selected != Selection)
				Selection = selected;
			
			if (SelectionRect != null && Selection.activeSelf)
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

		public void SetSelection(GameObject go)
		{
			var system = EventSystem;
			if (system == null)
				return;

			system.SetSelectedGameObject(go);
			Selection = go;
		}
	}
}