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
				var pooled = PoolingManager.Instance.TakeFromPool(Impact);
				
				var impact = pooled != null ? pooled.GetComponent<IImpact>() : Instantiate(Resources.Load<GameObject>($"Impacts/{Impact.Name}")).GetComponent<IImpact>();
				impact.Spawn(this, tr.position, tr.eulerAngles);
			}

			Pool();
		}

		public void Spawn(IWeapon source, Vector3 origin, Vector3 force)
		{
			Source = source;

			owner = Source.Owner;
			ownerName = owner.GetGameObject().name;
			
			transform.SetParent(World.World.Instance.Projectiles);
			
			gameObject.SetActive(true);
			poolDelayed().Forget();

			Rigidbody.MovePosition(origin);
			Rigidbody.AddForce(force, ForceMode.Impulse);
		}
		
		public void Pool()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		private async UniTask poolDelayed()
		{
			await UniTask.WaitForSeconds(Lifetime);
			
			Pool();
		}
	}
}