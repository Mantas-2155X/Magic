using AI.Interfaces;
using Projectiles.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public Rigidbody Rigidbody { get; private set; }
		
		public IWeapon Source { get; set; }

		public IAlive Owner { get; set; }
		
		public virtual float Range { get; set; }
		
		public virtual float Lifetime { get; set; }
		
		public virtual int Damage { get; set; }

		private bool spawned;
		private bool destroyed;

		private Vector3 startingPosition;

		public void FixedUpdate()
		{
			if (destroyed || !spawned)
				return;
			
			if (Vector3.Distance(startingPosition, transform.position) < Range)
				return;
			
			destroyed = true;
			Destroy(gameObject);
		}

		public void OnCollisionEnter(Collision collision)
		{
			if (destroyed)
				return;
			
			var rb = collision.rigidbody;
			if (rb != null)
			{
				var alive = rb.GetComponent<IAlive>();
				if (alive != null)
				{
					if (alive == Owner)
					{
						Debug.Log($"[BaseProjectile {Owner.GetGameObject().name}] Not colliding with owner");
						return;
					}
					
					alive.Damage(Damage, this);
				}
			}
			
			destroyed = true;
			Destroy(gameObject);
			
			Debug.Log($"[BaseProjectile {Owner.GetGameObject().name}] Collided with {collision.transform.name}");
		}

		public void Spawn(Vector3 origin, Vector3 force)
		{
			Rigidbody.MovePosition(origin);
			Rigidbody.AddForce(force, ForceMode.Impulse);
			
			startingPosition = origin;
			spawned = true;
			
			Destroy(gameObject, Lifetime);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}