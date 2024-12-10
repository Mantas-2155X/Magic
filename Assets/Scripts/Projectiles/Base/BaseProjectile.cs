//#define DEBUG_BaseProjectile

using System;
using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
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

		public virtual float Distance { get; private set; }
		public virtual float Damage { get; private set; }
		public virtual Type Impact { get; private set; }

		private CancellationTokenSource distanceToken;
		
		private IAlive owner;
		private string ownerName;

		private Vector3 startingPosition;
		
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

		public void Update()
		{
			var distance = Vector3.Distance(startingPosition, transform.position);
			if (distance < Distance)
				return;

			clearVelocityAndPool().Forget();
		}
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force, bool parent)
		{
			Source = source;

			owner = Source.Owner;
			ownerName = owner.GetGameObject().name;
			startingPosition = origin;

			var tr = transform;
			
			if (parent)
				tr.SetParent(World.World.Instance.Projectiles);
			
			tr.position = origin;
			tr.eulerAngles = Vector3.zero;
			
			gameObject.SetActive(true);

			Rigidbody.AddForce(force, ForceMode.Impulse);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		private async UniTask clearVelocityAndPool()
		{
			if (distanceToken != null)
			{
				if (distanceToken.IsCancellationRequested)
					return;
				
				distanceToken.Cancel();
			}
			
			gameObject.SetActive(false);

			await UniTask.NextFrame();

			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}