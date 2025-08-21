using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Components
{
	// TODO: Transfer non-shared objects from one scene to another
	// TODO: Adjust scenedata to have a "chain" list which lets you view saves for C while you're in A
	
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
			// Save everything in the current scene, this includes shared data that the new scene would have
			StateManager.Instance.Save(out var saveData, false, false);
			
			// Since we're loading the save file directly, trick it into using data for the current scene for the new scene
			saveData.Scene = Scene.Name;
			
			// Load the new scene and the save. Only the shared data is applied (and creations) since most of the stuff is outside shared space
			await StateManager.Instance.LoadAsync(saveData, false);
		}
	}
}