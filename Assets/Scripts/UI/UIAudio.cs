using Managers;
using Managers.Enums;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class UIAudio : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IPointerDownHandler, IPointerUpHandler, ISubmitHandler
	{
		[SerializeField]
		public Selectable Selectable;
		
		[SerializeField]
		public EUIAudio HoverEnter;
		
		[SerializeField]
		public EUIAudio HoverExit;
		
		[SerializeField]
		public EUIAudio Press;
		
		[SerializeField]
		public EUIAudio Release;
		
		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!Selectable.interactable)
				return;
			
			AudioManager.Instance.PlayUI(HoverEnter);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (!Selectable.interactable)
				return;

			AudioManager.Instance.PlayUI(HoverExit);
		}
		
		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData || !Selectable.interactable)
				return;
			
			AudioManager.Instance.PlayUI(HoverEnter);
		}
		
		public void OnPointerDown(PointerEventData eventData)
		{
			if (!Selectable.interactable)
				return;

			AudioManager.Instance.PlayUI(Press);
		}
		
		public void OnPointerUp(PointerEventData eventData)
		{
			if (!Selectable.interactable)
				return;

			AudioManager.Instance.PlayUI(Release);
		}
		
		public void OnSubmit(BaseEventData eventData)
		{
			if (!Selectable.interactable)
				return;

			AudioManager.Instance.PlayUI(Press);
		}
	}
}