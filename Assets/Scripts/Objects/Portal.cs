using Managers;
using UnityEngine;

namespace Objects
{
	public class Portal : MonoBehaviour
	{
		[SerializeField]
		public ParticleSystem System;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public void Spawn(Vector3 position)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Other);
				init = true;
			}
			
			thisTr.position = position;
			
			thisGo.SetActive(true);
			System.Play(true);
		}
		
		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), thisGo);
		}
	}
}