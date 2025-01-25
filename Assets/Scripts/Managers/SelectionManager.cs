using Managers.Events;
using UnityEngine;
using UnityEngine.EventSystems;

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

				var go = new GameObject("SelectionManager");
				DontDestroyOnLoad(go);

				instance = go.AddComponent<SelectionManager>();
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
				OnSelectionChangedEvent.Invoke(previousSelection, selection);
				Debug.Log($"selected {selection}");
			}
		}

		public void Update()
		{
			var system = EventSystem;
			if (system == null)
				return;
			
			var selected = system.currentSelectedGameObject;
			if (selected == Selection)
				return;
			
			Selection = selected;
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