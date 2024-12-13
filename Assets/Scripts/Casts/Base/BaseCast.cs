using Casts.Interfaces;
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

		[SerializeField]
		public float Rotation;
		
		public void Update()
		{
			transform.Rotate(Vector3.up, Rotation * Time.deltaTime);

			if (Source == null || Source.Owner == null)
				return;

			setPosition();
		}
		
		public void OnParticleSystemStopped()
		{
			gameObject.SetActive(false);
		}

		public void OnDisable()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		public void Spawn(IWeapon source, bool parent)
		{
			Source = source;
			
			if (parent)
				transform.SetParent(World.World.Instance.Other);
			
			setPosition();
			
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

		private void setPosition()
		{
			var tr = Source.Owner.GetGameObject().transform;
			transform.position = tr.position + Vector3.down * 0.95f;
		}
	}
}