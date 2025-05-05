using Managers;
using UnityEngine;

namespace Components
{
	public class Delete : MonoBehaviour
	{
		public void OnTriggerEnter(Collider other)
		{
			var rb = other.attachedRigidbody;
			var go = rb != null ? rb.gameObject : other.gameObject;
			
			var aiManager = AIManager.Instance;
			if (aiManager != null)
			{
				var player = aiManager.Player;
				if (player != null && player.Body.BodyCollider == other)
				{
					return;
				}
			}
			
			Debug.LogWarning($"[Delete] Object {go.name} fell out of the world and was destroyed");
			Destroy(go);
		}
	}
}