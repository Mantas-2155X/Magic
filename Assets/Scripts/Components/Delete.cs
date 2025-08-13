using Managers;
using UnityEngine;

namespace Components
{
	public class Delete : MonoBehaviour
	{
		public void OnTriggerEnter(Collider other)
		{
			var rb = other.attachedRigidbody;
			if (rb != null && rb == AIManager.Instance.Player.Body.Rigidbody)
				return;
			
			var go = rb != null ? rb.gameObject : other.gameObject;

			Debug.LogWarning($"[Delete] Object {go.name} fell out of the world and was destroyed");
			Destroy(go);
		}
	}
}