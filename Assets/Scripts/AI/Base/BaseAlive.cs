using AI.Events;
using AI.Interfaces;
using Objects;
using Tools;
using UnityEngine;
using Weapons.Interfaces;
using Random = UnityEngine.Random;

namespace AI.Base
{
	public class BaseAlive : MonoBehaviour, IAlive
	{
		public static readonly OnHealEvent OnHealEvent = new ();
		public static readonly OnDamageEvent OnDamageEvent = new ();
		public static readonly OnDeathEvent OnDeathEvent = new ();
		public static readonly OnSpawnEvent OnSpawnEvent = new ();
		
		private LayerMask previousExcludeLayers;
		
		public void OnCollisionEnter(Collision coll)
		{
			if (!IsAlive)
				return;

			var velocity = coll.relativeVelocity.y - Body.FallMinimumVelocity;
			if (velocity < 0f)
				return;

			var damage = Mathf.FloorToInt(Body.FallDamageMultiplier * (velocity * velocity));
			Damage(damage, null);
		}

		#region IAlive

		[field: SerializeField]
		public Body Body { get; private set; }
		
		public IWeapon Weapon { get; private set; }
		
		public virtual float CurrentSpeed { get; private set; }
		public float MaximumSpeed { get; private set; }

		public int CurrentHealth { get; private set; }
		public int StartingHealth { get; private set; }
		public int OverloadHealth { get; private set; }
		
		public bool IsAlive { get; private set; }
		public bool IsInvulnerable { get; private set; }
		public bool IsNoclip { get; private set; }
		public virtual bool IsWalking { get; private set; }

		public void SetInvulnerable(bool value)
		{
			if (!IsAlive || IsInvulnerable == value)
				return;
			
			IsInvulnerable = value;
		}
		public virtual void SetNoclip(bool value)
		{
			if (!IsAlive || IsNoclip == value)
				return;
			
			IsNoclip = value;

			Body.Rigidbody.useGravity = !value;
			Body.Collider.enabled = !value;
			
			if (value)
				previousExcludeLayers = Body.Rigidbody.excludeLayers;
			else
				Body.Rigidbody.excludeLayers = previousExcludeLayers;

			var feet = Body.Feet;
			for (var i = 0; i < feet.Length; i++)
				feet[i].GetComponent<Collider>().enabled = !value;
		}

		public virtual void TakeWeapon(IWeapon weapon)
		{
			DropWeapon();
			
			Weapon = weapon;
			Weapon?.Take(this);
		}
		public void DropWeapon()
		{
			Weapon?.Drop();
			Weapon = null;
		}

		public virtual void Spawn(int startingHealth, int overloadHealth, float maximumSpeed)
		{
			if (IsAlive)
				return;

			MaximumSpeed = maximumSpeed;

			CurrentHealth = startingHealth;
			StartingHealth = startingHealth;
			OverloadHealth = overloadHealth;

			IsAlive = true;
			
			OnSpawnEvent?.Invoke(this);
		}
		public virtual void Heal(int health, object source)
		{
			if (!IsAlive || health < 0)
				return;
			
			CurrentHealth += health;
			OnHealEvent?.Invoke(this, health, source);
			
			if (CurrentHealth >= OverloadHealth)
				Kill(this);
		}
		public virtual void Damage(int damage, object source)
		{
			if (!IsAlive || damage < 0 || IsInvulnerable)
				return;
			
			CurrentHealth -= damage;
			OnDamageEvent?.Invoke(this, damage, source);

			if (CurrentHealth > 0)
				return;
			
			Kill(source);
		}
		public virtual void Kill(object source)
		{
			if (!IsAlive)
				return;
			
			SetNoclip(false);
			DropWeapon();

			CurrentHealth = 0;
			IsAlive = false;
			
			Body.Rigidbody.constraints = RigidbodyConstraints.None;
			Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

			Body.Rigidbody.isKinematic = false;
			Body.Rigidbody.AddForce(Random.Range(-25f, 25f), 100f, Random.Range(-25f, 25f), ForceMode.Impulse);

			Body.Collider.material = null;
			
			var ragdolls = World.World.Instance.Ragdolls;
			
			var transforms = GetComponentsInChildren<Transform>();
			foreach (var tr in transforms)
			{
				var go = tr.gameObject;
				go.layer = 0;

				var coll = go.GetComponent<Collider>();
				if (coll == null)
					continue;

				coll.enabled = true;
				coll.excludeLayers = 0;
				
				go.AddComponent<HealingGib>();

				var rb = go.GetComponent<Rigidbody>();
				if (rb == null)
				{
					rb = go.AddComponent<Rigidbody>();
					rb.mass = 5;
				}
					
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.automaticInertiaTensor = false;
				rb.excludeLayers = 0;
				
				tr.parent = ragdolls;
			}
			
			OnDeathEvent?.Invoke(this, source);
		}

		public virtual bool IsGrounded()
		{
			if (IsNoclip)
				return false;
			
			var feet = Body.Feet;
			for (var i = 0; i < feet.Length; i++)
			{
				var foot = feet[i];
				if (!Physics.SphereCast(foot.position, 0.0925f, -foot.up, out _, 0.123f, ~LayerMaskTools.Mask2))
					continue;

				return true;
			}
			
			return false;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		#endregion
	}
}