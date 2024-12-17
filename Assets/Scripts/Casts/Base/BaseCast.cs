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
		
		public void Update()
		{
			if (Source == null)
				return;

			if (Rotation > 0f)
				transform.Rotate(Vector3.up, Rotation * Time.deltaTime);

			FollowOwner();
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
			
			var tr = transform;
			tr.SetParent(World.World.Instance.Other);
			
			FollowOwner();
			
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

		public void FollowOwner()
		{
			Transform tr;
			
			if (Source is IWeapon weapon && weapon.Owner != null)
				tr = weapon.Owner.GetGameObject().transform;
			else
				tr = Source.transform;
			
			transform.position = tr.position + Vector3.down * 0.95f;
		}
	}
}