using UnityEngine;

namespace UI
{
	public class Settings : MonoBehaviour
	{
		public void OnEnable()
		{
			transform.SetAsLastSibling();
		}

		public void OnCloseClicked()
		{
			Display(false);
		}

		public void Toggle()
		{
			Display(!isActiveAndEnabled);
		}
		
		public void Display(bool state)
		{
			if (state == isActiveAndEnabled)
				return;
			
			if (state)
			{
				var title = Title.Instance;
				if (title != null && !title.isActiveAndEnabled)
					title.Open();
			}

			gameObject.SetActive(state);
		}
	}
}