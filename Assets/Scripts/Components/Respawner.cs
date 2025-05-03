using System.Collections.Generic;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Components
{
	public class Respawner : MonoBehaviour
	{
		[SerializeField]
		public List<Collider> Objects;

		[SerializeField]
		public Transform SpawnPoint;
		
		public void OnTriggerEnter(Collider other)
		{
			if (!Objects.Contains(other))
				return;
			
			var tr = other.transform;
			
			tr.position = SpawnPoint.position;
			tr.rotation = SpawnPoint.rotation;
			
			var portal = ObjectManager.Instance.GetObject("OBJECT_PORTAL_NAME");
			ObjectManager.Instance.CreateObject(portal, tr.position, Vector3.zero);

			var rb = other.attachedRigidbody;
			if (rb == null)
				return;

			rb.position = SpawnPoint.position;
			rb.rotation = SpawnPoint.rotation;
			
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			
			ObjectManager.Instance.CreateObject(portal, SpawnPoint.position, Vector3.zero);
		}
	}
}