using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Components
{
	// TODO: Transfer non-shared objects from one scene to another
	// TODO: Transfer chained save datas (going from A to B to C and then back to A should remember and restore the data)
	
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
			StateManager.Instance.Save(out var saveData, false, true);
			
			// Since we're loading the save file directly, trick it into using data for the current scene for the new scene
			saveData.Scene = Scene.Name;
			
			// Load the new scene and the save. Only the shared data is applied (and creations) since most of the stuff is outside shared space
			await StateManager.Instance.LoadAsync(saveData, false);
		}
	}
}