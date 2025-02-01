using Cysharp.Threading.Tasks;
using Managers;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class SelectScroller : MonoBehaviour, ISelectHandler
	{
		[SerializeField]
		public MonoBehaviour Element;
		
		[SerializeField]
		public ScrollRect ScrollRect;

		[SerializeField]
		public bool ScrollOnStart;
		
		public void Start()
		{
			if (!ScrollOnStart)
				return;
			
			var sel = SelectionManager.Instance.Selection;
			if (sel == null || sel != gameObject)
				return;

			// Do it no matter the device when it comes alive
			OnSelect(new AxisEventData(EventSystem.current));
		}

		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData)
				return;
			
			ScrollRect.ScrollToCenterDelayed((RectTransform)transform, Element).Forget();
		}
	}
}