using Managers;
using UnityEngine;

namespace Objects
{
	public class Portal : MonoBehaviour
	{
		[SerializeField]
		public ParticleSystem System;

		public void Spawn(Vector3 position, bool parent)
		{
			var tr = transform;
			
			if (parent)
				tr.SetParent(World.World.Instance.Other);

			tr.position = position;
			
			gameObject.SetActive(true);
			System.Play(true);
		}
		
		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}