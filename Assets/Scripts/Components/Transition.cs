using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Components
{
	// TODO: Transfer objects from one scene to another
	// TODO: Transfer save datas including the old ones from any previous transitions as well
	
	public class Transition : MonoBehaviour
	{
		[SerializeField]
		public SceneData Scene;
		
		public void Trigger()
		{
			doTransition().Forget();
		}

		private async UniTaskVoid doTransition()
		{
			StateManager.Instance.Save(out var saveData, false, true);
			
			await SceneManager.Instance.ChangeSceneAsync(Scene, false, false, true, waitForGI: false);
			
			StateManager.Instance.Load(saveData, false);
		}
	}
}