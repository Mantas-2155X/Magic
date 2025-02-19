using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class Scale : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
	{
		[SerializeField]
		public RectTransform ScaleTarget;

		[SerializeField]
		public bool UseThreshold;
		
		[SerializeField]
		public Vector2 MinimumSize;
		
		public static readonly List<Scale> Instances = new ();

		private Vector2 beginPosition;
		private RectTransform canvas;
		
		private const float borderAmount = 5f;

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
			ScaleTarget.SetAsLastSibling();
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

		public void OnEndDrag(PointerEventData eventData)
		{
			ClampScaleToScreenResolution();
		}
		
		public void OnInitializePotentialDrag(PointerEventData eventData)
		{
			eventData.useDragThreshold = UseThreshold;
		}

		public void ClampScaleToScreenResolution()
		{
			var canvasRect = canvas.rect;
			var targetRect = ScaleTarget.rect;

			var maxHeight = canvasRect.height - (borderAmount * 2f);
			var maxWidth = canvasRect.width - (borderAmount * 2f);
			
			if (targetRect.height > maxHeight)
				ScaleTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
			
			if (targetRect.width > maxWidth)
				ScaleTarget.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
		}
	}
}