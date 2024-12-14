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
		
		[field: SerializeField]
		public Collider Collider { get; private set; }

		public IWeapon Source { get; private set; }

		public virtual float Distance { get; private set; }
		public virtual float Damage { get; private set; }
		public virtual Type Impact { get; private set; }

		private CancellationTokenSource distanceToken;
		
		private Vector3 startingPosition;

		private Collider ignoreCollider;
		
		public void OnCollisionEnter(Collision collision)
		{
			var coll = collision.collider;
			if (coll != null)
			{
				if (coll.TryGetComponent<IAlive>(out var alive))
				{
					alive.Damage(Damage, this);
				}
				else if (coll.TryGetComponent<IBreakable>(out var breakable))
				{
					breakable.Damage(Damage, this);
				}
			}

			var contact = collision.contacts[0];
			ObjectManager.Instance.CreateImpact(Impact, this, transform.position, Quaternion.FromToRotation(Vector3.up, contact.normal));
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

			startingPosition = origin;
			ignoreCollider = source.Owner.Body.Collider;
			
			Physics.IgnoreCollision(ignoreCollider, Collider, true);

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
			
			Physics.IgnoreCollision(ignoreCollider, Collider, false);
			
			gameObject.SetActive(false);

			await UniTask.NextFrame();

			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}