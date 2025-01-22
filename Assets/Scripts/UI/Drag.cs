using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class Drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IInitializePotentialDragHandler
	{
		[SerializeField]
		public RectTransform DragTarget;

		[SerializeField]
		public bool UseThreshold;
		
		public void OnBeginDrag(PointerEventData eventData)
		{
			DragTarget.SetAsLastSibling();
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			DragTarget.position += new Vector3(eventData.delta.x, eventData.delta.y);
		}
		
		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			eventData.useDragThreshold = UseThreshold;
		}
	}
}