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
		
		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData)
				return;
			
			var sceneSelect = Title.Instance.SceneSelect;
			sceneSelect.ScrollRect.ScrollToCenter((RectTransform)transform, sceneSelect);
		}
	}
}