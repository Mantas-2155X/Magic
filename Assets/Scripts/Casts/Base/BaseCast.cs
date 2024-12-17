using Casts.Interfaces;
using Managers;
using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Base
{
	public class BaseCast : MonoBehaviour, ICast
	{
		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		
		[SerializeField]
		public float Rotation;

		private Transform ownerTr;
		private Transform thisTr;
		
		public void Update()
		{
			if (Source == null)
				return;

			if (Rotation > 0f)
				thisTr.Rotate(Vector3.up, Rotation * Time.deltaTime);

			thisTr.position = ownerTr.position + Vector3.down * 0.95f;
		}
		
		public void OnParticleSystemStopped()
		{
			gameObject.SetActive(false);
		}

		public void OnDisable()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		public void Spawn(Component source)
		{
			Source = source;
			
			if (Source is IWeapon weapon && weapon.Owner != null)
				ownerTr = weapon.Owner.GetGameObject().transform;
			else
				ownerTr = Source.transform;
			
			thisTr = transform;
			thisTr.SetParent(World.World.Instance.Other);
			
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
	}
}