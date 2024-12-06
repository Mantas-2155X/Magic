using System;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Impacts.Interfaces;
using Managers;
using Projectiles.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public Rigidbody Rigidbody { get; private set; }
		
		public IWeapon Source { get; private set; }

		[field: SerializeField]
		public virtual float Lifetime { get; private set; }
		[field: SerializeField]
		public virtual int Damage { get; private set; }

		public virtual Type Impact { get; private set; }
		
		private IAlive owner;
		private string ownerName;
		
		public void OnCollisionEnter(Collision collision)
		{
			var rb = collision.rigidbody;
			if (rb != null)
			{
				var alive = rb.GetComponent<IAlive>();
				if (alive != null)
				{
					if (alive == owner)
					{
						Debug.Log($"[BaseProjectile {ownerName}] Not colliding with owner");
						return;
					}
					
					alive.Damage(Damage, this);
				}
			}

			if (Impact != null)
			{
				var tr = transform;
				var pooled = PoolingManager.Instance.TakeFromPool(Impact, false);
				
				var impact = pooled != null ? pooled.GetComponent<IImpact>() : Instantiate(Resources.Load<GameObject>($"Impacts/{Impact.Name}")).GetComponent<IImpact>();
				impact.Spawn(this, tr.position, tr.eulerAngles);
			}

			clearVelocityAndPool().Forget();
		}

		public void Spawn(IWeapon source, Vector3 origin, Vector3 force)
		{
			Source = source;

			owner = Source.Owner;
			ownerName = owner.GetGameObject().name;

			var tr = transform;
			tr.SetParent(World.World.Instance.Projectiles);
			tr.position = origin;
			tr.eulerAngles = Vector3.zero;
			
			gameObject.SetActive(true);
			waitLifetimeAndPool().Forget();

			Rigidbody.AddForce(force, ForceMode.Impulse);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		private async UniTask waitLifetimeAndPool()
		{
			await UniTask.WaitForSeconds(Lifetime);
			await clearVelocityAndPool();
		}

		private async UniTask clearVelocityAndPool()
		{
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}