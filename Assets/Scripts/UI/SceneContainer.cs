using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
	public class SceneContainer : MonoBehaviour, ISelectHandler
	{
		[SerializeField]
		public Button Button;

		[SerializeField]
		public Image Image;
		
		[SerializeField]
		public Localizer Localizer;

		[SerializeField]
		public TMP_Text Date;

		[SerializeField]
		public GameObject AutoSave;
		
		public ScrollRect ScrollRect;
		public MonoBehaviour Parent;
		
		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData)
				return;
			
			ScrollRect.ScrollToCenter((RectTransform)transform, Parent);
		}
	}
}