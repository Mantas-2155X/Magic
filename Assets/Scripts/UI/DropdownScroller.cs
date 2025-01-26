using Cysharp.Threading.Tasks;
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

		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is PointerEventData)
				return;
			
			ScrollRect.ScrollToCenterDelayed((RectTransform)transform, Dropdown).Forget();
		}
	}
}