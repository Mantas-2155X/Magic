using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Scenes
{
	public class SplashScene : MonoBehaviour
	{
		public void Awake()
		{
			SceneManager.Instance.ChangeScene(ObjectManager.Instance.GetData<SceneData>("SCENE_TITLE_NAME"), true, true, false);
		}
	}
}