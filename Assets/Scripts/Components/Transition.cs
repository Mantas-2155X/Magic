using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using State.Interfaces;
using Tools;
using UnityEngine;

namespace Components
{
	// TODO: Non-shared object should be removed from original scene if its taken to a different scene
	// TODO: Non-shared object does not persist outside of shared areas
	
	public class Transition : MonoBehaviour
	{
		[SerializeField]
		public SceneData Scene;

		[SerializeField]
		public Vector3 SharedCenter;
		
		[SerializeField]
		public Vector3 SharedExtents;

		private readonly Collider[] transferColliders = new Collider[256];
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.DrawWireCube(SharedCenter, SharedExtents);
		}
#endif
		
		public void Trigger()
		{
			doTransition().Forget();
		}

		private async UniTaskVoid doTransition()
		{
			var saveables = new List<ISaveable>();
			
			var size = Physics.OverlapBoxNonAlloc(SharedCenter, SharedExtents / 2f, transferColliders);
			for (var i = 0; i < size; i++)
			{
				var transferCollider = transferColliders[i];
				var transferRigidBody = transferCollider.attachedRigidbody;

				ISaveable saveable = null;

				if (transferRigidBody != null)
					saveable = transferRigidBody.GetComponent<ISaveable>();

				if (saveable.IsNull())
					saveable = transferCollider.GetComponent<ISaveable>();

				if (saveable.IsNull())
				{
					Debug.LogWarning($"[Transition] Saveable on {TransformTools.GetFullPath(transferCollider.transform)} was not found, skipping");
					continue;
				}

				if (!saveable!.ShouldTransfer)
				{
					Debug.LogWarning($"[Transition] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(transferCollider.transform)} is not marked transferrable, skipping");
					continue;
				}
				
				if (saveables.Contains(saveable))
					continue;
				
				saveable.Transferred = true;
				saveable.ExternallySpawned = true;
				
				saveables.Add(saveable);
			}

			// Save everything in the current scene, this includes shared data that the new scene would have
			StateManager.Instance.Save(out var saveData, false, false);
			
			// Since we're loading the save file for this scene on a different scene, trick it into thinking it's on the correct scene
			saveData.Scene = Scene.Name;

			for (var i = 0; i < saveables.Count; i++)
			{
				var saveable = saveables[i];
				
				if (!saveData.Items.TryGetValue(saveable.ObjectID, out var item))
				{
					Debug.LogWarning($"[Transition] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(saveable.GetTransform())} is not in the save, skipping");
					continue;
				}

				// Saveable in shared area is transferred to the new scene
				item.Scene = Scene.Name;
				
				Debug.Log($"[Transition] Transferred saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(saveable.GetTransform())}");
			}
			
			// Load the new scene and the save. Only the shared data is applied (and creations) since most of the stuff is outside shared space
			await StateManager.Instance.LoadAsync(saveData, false);
		}
	}
}