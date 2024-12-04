using AI.Interfaces;
using Impacts.Interfaces;
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
		public virtual float Range { get; private set; }
		[field: SerializeField]
		public virtual float Lifetime { get; private set; }
		[field: SerializeField]
		public virtual int Damage { get; private set; }
		[field: SerializeField]
		public virtual string Impact { get; private set; }

		private IAlive owner;
		private string ownerName;
		
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
					if (alive == owner)
					{
						Debug.Log($"[BaseProjectile {ownerName}] Not colliding with owner");
						return;
					}
					
					alive.Damage(Damage, this);
				}
			}

			if (Impact != "")
			{
				var go = Instantiate(Resources.Load<GameObject>($"Impacts/{Impact}"));
				var im = go.GetComponent<IImpact>();
				im.Spawn(this, transform.position, transform.eulerAngles);
			}
			
			destroyed = true;
			Destroy(gameObject);
			
			Debug.Log($"[BaseProjectile {ownerName}] Collided with {collision.transform.name}");
		}

		public void Spawn(IWeapon source, Vector3 origin, Vector3 force)
		{
			Source = source;
			startingPosition = origin;

			owner = Source.Owner;
			ownerName = owner.GetGameObject().name;
			
			Rigidbody.MovePosition(origin);
			Rigidbody.AddForce(force, ForceMode.Impulse);
			
			spawned = true;
			
			Destroy(gameObject, Lifetime);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}