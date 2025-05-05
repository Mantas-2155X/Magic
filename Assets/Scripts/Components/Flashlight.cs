using UnityEngine;

namespace Components
{
	public class Flashlight : MonoBehaviour
	{
		public static Flashlight Instance;
		
		[SerializeField]
		public Light Light;

		public void OnEnable()
		{
			Instance = this;
		}

		public void Toggle()
		{
			Light.enabled = !Light.enabled;
		}

		public void Enable()
		{
			Light.enabled = true;
		}

		public void Disable()
		{
			Light.enabled = false;
		}
	}
}