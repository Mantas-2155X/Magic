//#define DEBUG_BaseProjectile

using System;
using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Impacts.Interfaces;
using Managers;
using Objects.Interfaces;
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

		private CancellationTokenSource lifetimeToken;
		
		private IAlive owner;
		private string ownerName;
		
		public void OnCollisionEnter(Collision collision)
		{
			var coll = collision.collider;
			if (coll != null)
			{
				if (coll.TryGetComponent<IAlive>(out var alive))
				{
					if (alive == owner)
					{
#if DEBUG_BaseProjectile
						Debug.Log($"[BaseProjectile {ownerName}] Not colliding with owner");
#endif
						return;
					}
					
					alive.Damage(Damage, this);
				}
				else if (coll.TryGetComponent<IBreakable>(out var breakable))
				{
					breakable.Damage(Damage, this);
				}
			}

			ObjectManager.Instance.CreateImpact(Impact, this, transform.position, transform.eulerAngles);
			clearVelocityAndPool().Forget();
		}

		public void Spawn(IWeapon source, Vector3 origin, Vector3 force, bool parent)
		{
			Source = source;

			owner = Source.Owner;
			ownerName = owner.GetGameObject().name;

			var tr = transform;
			
			if (parent)
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
			lifetimeToken?.Dispose();
			lifetimeToken = new CancellationTokenSource();
			
			await UniTask.WaitForSeconds(Lifetime);
			await clearVelocityAndPool();
		}

		private async UniTask clearVelocityAndPool()
		{
			if (lifetimeToken != null)
			{
				if (lifetimeToken.IsCancellationRequested)
					return;
				
				lifetimeToken.Cancel();
			}
			
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}