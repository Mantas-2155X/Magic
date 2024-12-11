using AI.Enums;
using AI.Events;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
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
		public static readonly OnManaGenerateEvent OnManaGenerateEvent = new ();
		public static readonly OnManaUseEvent OnManaUseEvent = new ();
		public static readonly OnDeathEvent OnDeathEvent = new ();
		public static readonly OnSpawnEvent OnSpawnEvent = new ();
		
		private LayerMask previousExcludeLayers;

		#region MonoBehaviour

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

		#endregion
		
		#region IAlive

		[field: SerializeField]
		public Body Body { get; private set; }
		
		public IWeapon Weapon { get; private set; }
		
		public virtual float CurrentSpeed { get; private set; }
		public float MaximumSpeed { get; private set; }

		public float CurrentHealth { get; private set; }
		public float StartingHealth { get; private set; }
		public float OverloadHealth { get; private set; }
		public float RegenerateHealth { get; private set; }
		
		public float CurrentMana { get; private set; }
		public float StartingMana { get; private set; }
		public float OverloadMana { get; private set; }
		public float RegenerateMana { get; private set; }

		public EMovementType MovementType { get; private set; }

		public bool IsAlive { get; private set; }
		public bool IsInvulnerable { get; private set; }
		public bool IsPowerful { get; private set; }
		public virtual bool IsWalking { get; private set; }

		public void SetInvulnerable(bool value)
		{
			if (!IsAlive || IsInvulnerable == value)
				return;
			
			IsInvulnerable = value;
		}
		public void SetPowerful(bool value)
		{
			if (!IsAlive || IsPowerful == value)
				return;
			
			IsPowerful = value;
		}
		public virtual void SetMovementType(EMovementType value)
		{
			if (!IsAlive || MovementType == value)
				return;
			
			MovementType = value;

			Body.Rigidbody.useGravity = MovementType == EMovementType.Normal;
			Body.Collider.enabled = MovementType == EMovementType.Normal;
			
			if (MovementType != EMovementType.Normal)
				previousExcludeLayers = Body.Rigidbody.excludeLayers;
			else
				Body.Rigidbody.excludeLayers = previousExcludeLayers;

			var feet = Body.Feet;
			for (var i = 0; i < feet.Length; i++)
				feet[i].GetComponent<Collider>().enabled = MovementType == EMovementType.Normal;
		}

		public virtual void TakeWeapon(IWeapon weapon)
		{
			DropWeapon();
			
			Weapon = weapon;
			Weapon?.Take(this);
		}
		public virtual void DropWeapon()
		{
			Weapon?.Drop();
			Weapon = null;
		}

		public virtual void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed)
		{
			if (IsAlive)
				return;

			MaximumSpeed = maximumSpeed;

			CurrentHealth = startingHealth;
			StartingHealth = startingHealth;
			OverloadHealth = overloadHealth;
			RegenerateHealth = regenerateHealth;

			CurrentMana = startingMana;
			StartingMana = startingMana;
			OverloadMana = overloadMana;
			RegenerateMana = regenerateMana;

			IsAlive = true;
			OnSpawnEvent?.Invoke(this);
			
			regenerateLoop().Forget();
		}
		public virtual void Heal(float health, object source, bool clamp = false)
		{
			if (!IsAlive || health < 0)
				return;
			
			if (clamp)
			{
				if (CurrentHealth >= StartingHealth)
					return;

				if (CurrentHealth + health >= StartingHealth)
					health = StartingHealth - CurrentHealth;
			}
			
			CurrentHealth += health;
			OnHealEvent?.Invoke(this, health, source);
			
			if (CurrentHealth >= OverloadHealth)
				Kill(this);
		}
		public virtual void Damage(float damage, object source)
		{
			if (!IsAlive || damage < 0 || IsInvulnerable)
				return;
			
			CurrentHealth -= damage;
			OnDamageEvent?.Invoke(this, damage, source);

			if (CurrentHealth > 0)
				return;
			
			Kill(source);
		}
		public virtual void GenerateMana(float mana, object source, bool clamp = false)
		{
			if (!IsAlive || mana < 0)
				return;

			if (clamp)
			{
				if (CurrentMana >= StartingMana)
					return;

				if (CurrentMana + mana >= StartingMana)
					mana = StartingMana - CurrentMana;
			}
			
			CurrentMana += mana;
			OnManaGenerateEvent?.Invoke(this, mana, source);
			
			if (CurrentMana >= OverloadMana)
				Kill(this);
		}
		public virtual void UseMana(float mana, object source)
		{
			if (!IsAlive || mana < 0 || IsPowerful)
				return;
			
			CurrentMana -= mana;
			OnManaUseEvent?.Invoke(this, mana, source);
		}

		public virtual void Kill(object source)
		{
			if (!IsAlive)
				return;
			
			SetMovementType(EMovementType.Normal);
			DropWeapon();

			CurrentHealth = 0;
			CurrentMana = 0;
			IsAlive = false;
			
			Body.Rigidbody.constraints = RigidbodyConstraints.None;
			Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

			Body.Rigidbody.isKinematic = false;
			Body.Rigidbody.AddForce(Random.Range(-25f, 25f), 100f, Random.Range(-25f, 25f), ForceMode.Impulse);

			Body.Collider.material = null;

			var ragdolls = World.World.Instance.Ragdolls;
			var length = Body.Gibs.Length;

			for (var i = 0; i < length; i++)
			{
				var gib = Body.Gibs[i];
				gib.enabled = true;

				var go = gib.gameObject;
				go.layer = 0;

				var coll = go.GetComponent<Collider>();
				coll.excludeLayers = 0;
				coll.material = null;
				coll.enabled = true;

				var rb = i == length - 1 ? Body.Rigidbody : go.AddComponent<Rigidbody>();
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.automaticInertiaTensor = false;
				rb.excludeLayers = 0;
				rb.mass = 5;

				var tr = go.transform;
				tr.parent = ragdolls;
			}

			OnDeathEvent?.Invoke(this, source);
		}

		public virtual bool IsGrounded()
		{
			if (MovementType != EMovementType.Normal)
				return false;
			
			var feet = Body.Feet;
			for (var i = 0; i < feet.Length; i++)
			{
				var foot = feet[i];
				if (!Physics.SphereCast(foot.position, 0.1375f, Vector3.down, out _, 0.15f, ~LayerMaskTools.Mask2))
					continue;

				return true;
			}
			
			return false;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		private async UniTask regenerateLoop()
		{
			while (IsAlive)
			{
				await UniTask.WaitForSeconds(0.5f);

				GenerateMana(RegenerateMana, this, true);
				Heal(RegenerateHealth, this, true);
			}
		}
		
		#endregion
	}
}