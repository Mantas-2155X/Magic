using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using State.Interfaces;
using Tools;
using UnityEngine;

namespace Components
{
	public class Transition : MonoBehaviour
	{
		[SerializeField]
		public SceneData Scene;

		[SerializeField]
		public Vector3 SharedCenter;
		
		[SerializeField]
		public Vector3 SharedExtents;

		private readonly Collider[] transferColliders = new Collider[256];
		private bool triggered;
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.DrawWireCube(SharedCenter, SharedExtents);
		}
#endif
		
		public void Trigger()
		{
			triggered = true;
		}
		
		public void LateUpdate()
		{
			if (!triggered)
				return;

			triggered = false;
			doTransition().Forget();
		}

		private async UniTaskVoid doTransition()
		{
			var size = Physics.OverlapBoxNonAlloc(SharedCenter, SharedExtents / 2f, transferColliders, Quaternion.identity, ~LayerMask.GetMask("Broken"));
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
					continue;

				if (!saveable!.ShouldSave)
					continue;
				
				if (!saveable.ShouldTransfer)
				{
					Debug.LogWarning($"[Transition] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(transferCollider.transform)} is not marked transferrable, skipping");
					continue;
				}
				
				saveable.ExternallySpawned = true;
				saveable.TransferredScene = Scene.Name;
			}

			// Save everything in the current scene, this includes shared data that the new scene would have
			StateManager.Instance.Save(out var saveData, false, false);
			
			// Since we're loading the save file for this scene on a different scene, trick it into thinking it's on the correct scene
			saveData.Scene = Scene.Name;
			
			// Load the new scene and the save. Only the shared data is applied (and creations) since most of the stuff is outside shared space
			await StateManager.Instance.LoadAsync(saveData, false);
		}
	}
}