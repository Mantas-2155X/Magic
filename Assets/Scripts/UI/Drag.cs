using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class Drag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
	{
		[SerializeField]
		public RectTransform DragTarget;

		[SerializeField]
		public bool UseThreshold;
		
		public static readonly List<Drag> Instances = new ();

		private RectTransform canvas;
		
		private const float borderAmount = 5f;
		private const float bottomRightHeaderAmount = 41f;

		public void Awake()
		{
			canvas = (RectTransform)GetComponentInParent<Canvas>().transform;
			Instances.Add(this);
		}

		public void OnDestroy()
		{
			Instances.Remove(this);
		}
		
		public void OnBeginDrag(PointerEventData eventData)
		{
			DragTarget.SetAsLastSibling();
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			DragTarget.position += new Vector3(eventData.delta.x, eventData.delta.y);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			ClampPositionToScreenBounds();
		}
		
		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			eventData.useDragThreshold = UseThreshold;
		}
		
		public void ClampPositionToScreenBounds()
		{
			var canvasRect = canvas.rect;
			
			// Left
			if (DragTarget.anchoredPosition.x + canvasRect.width / 2f < borderAmount)
				DragTarget.anchoredPosition = new Vector3(-(canvasRect.width / 2f) + borderAmount, DragTarget.anchoredPosition.y);
			
			// Right
			if (DragTarget.anchoredPosition.x > (canvasRect.width / 2f) - bottomRightHeaderAmount)
				DragTarget.anchoredPosition = new Vector3((canvasRect.width / 2f) - bottomRightHeaderAmount, DragTarget.anchoredPosition.y);
			
			// Top
			if (DragTarget.anchoredPosition.y > canvasRect.height / 2f - borderAmount)
				DragTarget.anchoredPosition = new Vector3(DragTarget.anchoredPosition.x, (canvasRect.height / 2f) - borderAmount);
			
			// Bottom
			if (DragTarget.anchoredPosition.y < -(canvasRect.height / 2f) + bottomRightHeaderAmount)
				DragTarget.anchoredPosition = new Vector3(DragTarget.anchoredPosition.x, -(canvasRect.height / 2f) + bottomRightHeaderAmount);
		}
	}
}