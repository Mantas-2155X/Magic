using Managers;
using UnityEngine;

namespace Objects
{
	public class Portal : MonoBehaviour
	{
		[SerializeField]
		public ParticleSystem System;

		public void Spawn(Vector3 position)
		{
			transform.position = position;
			gameObject.SetActive(true);
			System.Play();
		}
		
		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}