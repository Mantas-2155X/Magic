using Cysharp.Threading.Tasks;
using Managers;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class DropdownScroller : MonoBehaviour, ISelectHandler
	{
		[SerializeField]
		public TMP_Dropdown Dropdown;
		
		[SerializeField]
		public ScrollRect ScrollRect;

		public void Start()
		{
			var sel = SelectionManager.Instance.Selection;
			if (sel == null || sel.gameObject != gameObject)
				return;

			// Do it no matter the device when it comes alive
			OnSelect(new AxisEventData(EventSystem.current));
		}

		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData)
				return;
			
			ScrollRect.ScrollToCenterDelayed((RectTransform)transform, Dropdown).Forget();
		}
	}
}