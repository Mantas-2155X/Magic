using System;
using Casts.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Base
{
	public class BaseCast : MonoBehaviour, ICast
	{
		[field: SerializeField]
		public ParticleSystem System { get; private set; }

		public IWeapon Source { get; private set; }

		public void OnParticleSystemStopped()
		{
			gameObject.SetActive(false);
		}

		public void OnDisable()
		{
			poolDelayed().Forget();
		}
		
		public void Spawn(IWeapon source)
		{
			Source = source;

			var tr = transform;
			tr.SetParent(Source.Owner.GetGameObject().transform);
			
			tr.localPosition = Vector3.down * 0.98f;
			tr.localEulerAngles = Vector3.zero;
			
			gameObject.SetActive(true);
			System.Play(true);
		}

		public void StopParticles()
		{
			System.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}

		public GameObject GetGameObject()
		{
			return gameObject;
		}

		private async UniTask poolDelayed()
		{
			await UniTask.NextFrame();
			
			transform.SetParent(World.World.Instance.Other);
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}