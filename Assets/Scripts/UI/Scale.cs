using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class Scale : MonoBehaviour, IBeginDragHandler, IDragHandler
	{
		[SerializeField]
		public RectTransform ScaleTarget;

		[SerializeField]
		public Vector2 MinimumSize;
		
		private Vector2 beginPosition;
		
		public void OnBeginDrag(PointerEventData eventData)
		{
			beginPosition = eventData.position;
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			var currentPosTransform = (Vector2)ScaleTarget.InverseTransformVector(eventData.position);
			var beginPosTransform = (Vector2)ScaleTarget.InverseTransformVector(beginPosition);
			
			var delta = currentPosTransform - beginPosTransform;

			var newX = ScaleTarget.rect.width + delta.x;
			var newY = ScaleTarget.rect.height - delta.y;
            
			if (newY >= MinimumSize.y)
			{
				ScaleTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newY);
				beginPosition.y = eventData.position.y;
			}
			
			if (newX >= MinimumSize.x)
			{
				ScaleTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newX);
				beginPosition.x = eventData.position.x;
			}
		}
	}
}