using Managers;
using UnityEngine;

namespace Scenes
{
	public class SplashScene : MonoBehaviour
	{
		public void Awake()
		{
			SceneManager.Instance.ChangeScene(ObjectManager.Instance.GetScene("SCENE_TITLE_NAME"), true, true, false);
		}
	}
}