using Managers;
using UnityEngine;

namespace Scenes
{
	public class SplashScene : MonoBehaviour
	{
		public void Awake()
		{
			SceneManager.Instance.ChangeScene("Title", true, true, false);
		}
	}
}