using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class Drag : MonoBehaviour, IDragHandler
	{
		[SerializeField]
		public RectTransform DragTarget;
		
		public void OnDrag(PointerEventData eventData)
		{
			DragTarget.position += new Vector3(eventData.delta.x, eventData.delta.y);
		}
	}
}