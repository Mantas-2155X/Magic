using UI;
using UnityEngine;

namespace Scenes
{
	public class TitleScene : MonoBehaviour
	{
		public void Start()
		{
			var title = Title.Instance;
			title.Open();

			var console = title.Console;
			if (console.isActiveAndEnabled)
			{
				// Take focus and select the input field
				console.OnEnable();
			}
		}
	}
}