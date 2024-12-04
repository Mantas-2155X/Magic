using System.Collections.Generic;
using AI.Events;
using AI.Interfaces;
using Objects;
using UnityEngine;
using Weapons.Interfaces;
using Random = UnityEngine.Random;

namespace AI.Base
{
	public class BaseAlive : MonoBehaviour, IAlive
	{
		[SerializeField]
		public Rigidbody Rigidbody;

		[SerializeField]
		public Collider Collider;
		
		[SerializeField]
		public Renderer[] Eyes;
		
		[SerializeField]
		public Transform[] Shoulders;
		
		[SerializeField]
		public Transform[] Legs;

		[SerializeField]
		public Collider[] Feet;
		
		[SerializeField]
		public Vector2 SwayAngles = new (30f, 15f);

		[SerializeField]
		public float SwaySpeedMultiplier = 1f;

		[SerializeField]
		public float BlinkEvery = 3f;
		
		[SerializeField]
		public float BlinkDuration = 0.2f;

		[SerializeField]
		public float BlinkVariation = 2f;
		
		[HideInInspector]
		public bool ShouldSway;
		
		public static readonly OnHealEvent OnHealEvent = new ();
		public static readonly OnDamageEvent OnDamageEvent = new ();
		public static readonly OnDeathEvent OnDeathEvent = new ();
		public static readonly OnSpawnEvent OnSpawnEvent = new ();
		
		private readonly List<ContactPoint> contactPoints = new ();
		private readonly List<Collider> collidingState = new ();

		private LayerMask previousExcludeLayers;

		private bool swayDirection;
		private bool blinking;
		
		private float blinkStartTime;
		private float blinkFinishTime;

		#region MonoBehaviour

		public virtual void Update()
		{
			if (!IsAlive)
				return;
			
			if (blinking && Time.time >= blinkStartTime + BlinkDuration)
			{
				foreach (var eye in Eyes)
					eye.material.color = EyesColor;
				
				blinking = false;
				blinkFinishTime = Time.time + Random.Range(-BlinkVariation, BlinkVariation);
			}
			else if (!blinking && Time.time >= blinkFinishTime + BlinkEvery)
			{
				foreach (var eye in Eyes)
					eye.material.color = Color.black;
				
				blinking = true;
				blinkStartTime = Time.time;
			}
			
			if (ShouldSway)
				swayLimbs();
		}

		public void OnCollisionStay(Collision collision)
		{
			var count = collision.GetContacts(contactPoints);
			for (var i = 0; i < count; i++)
			{
				var contactPoint = contactPoints[i];
				collidingState.Add(contactPoint.thisCollider);
			}
		}
		
		private void OnCollisionExit(Collision _)
		{
			collidingState.Clear();
		}

		#endregion

		private void swayLimbs()
		{
			var incrementAmount = CurrentSpeed * SwaySpeedMultiplier * Time.deltaTime;
			
			if (swayDirection)
				incrementAmount = -incrementAmount;
			
			var incrementShoulders = new Vector3(incrementAmount * SwayAngles.x, 0, 0);
			var incrementLegs = new Vector3(incrementAmount * SwayAngles.y, 0, 0);

			Shoulders[0].localEulerAngles += incrementShoulders;
			Shoulders[1].localEulerAngles -= incrementShoulders;
			
			Legs[0].localEulerAngles -= incrementLegs;
			Legs[1].localEulerAngles += incrementLegs;

			var currentAngle = Shoulders[0].localEulerAngles.x;
			
			if (currentAngle > 180)
				currentAngle -= 360;

			if (!IsWalking && Mathf.Abs(currentAngle) < 1.5f)
			{
				ShouldSway = false;
				resetLimbs();
				return;
			}
			
			if (currentAngle > SwayAngles.x || currentAngle < -SwayAngles.x)
				swayDirection = !swayDirection;
			
			clampLimbsSway();
		}

		private void clampLimbsSway()
		{
			foreach (var shoulder in Shoulders)
			{
				var currentAngle = shoulder.localEulerAngles.x;
				
				if (currentAngle > 180)
					currentAngle -= 360;
				
				currentAngle = Mathf.Clamp(currentAngle, -SwayAngles.x, SwayAngles.x);
				shoulder.localEulerAngles = new Vector3(currentAngle, 0, 0);
			}
			
			foreach (var leg in Legs)
			{
				var currentAngle = leg.localEulerAngles.x;
				
				if (currentAngle > 180)
					currentAngle -= 360;
				
				currentAngle = Mathf.Clamp(currentAngle, -SwayAngles.y, SwayAngles.y);
				leg.localEulerAngles = new Vector3(currentAngle, 0, 0);
			}
		}

		private void resetLimbs()
		{
			foreach (var shoulder in Shoulders)
				shoulder.localEulerAngles = Vector3.zero;
			
			foreach (var leg in Legs)
				leg.localEulerAngles = Vector3.zero;
		}
		
		#region IAlive

		[field: SerializeField]
		public Transform WeaponContainer { get; private set; }
		public IWeapon Weapon { get; private set; }

		public virtual Color EyesColor { get; private set; }

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
		public void SetNoclip(bool value)
		{
			if (!IsAlive || IsNoclip == value)
				return;
			
			IsNoclip = value;

			Rigidbody.useGravity = !value;
			Collider.enabled = !value;

			for (var i = 0; i < Feet.Length; i++)
				Feet[i].enabled = !value;
			
			if (value)
			{
				collidingState.Clear();
				previousExcludeLayers = Rigidbody.excludeLayers;
			}
			else
			{
				Rigidbody.excludeLayers = previousExcludeLayers;
			}
		}

		public void TakeWeapon(IWeapon weapon)
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
			
			Rigidbody.constraints = RigidbodyConstraints.None;

			Rigidbody.isKinematic = false;
			Rigidbody.AddForce(Random.Range(-25f, 25f), 100f, Random.Range(-25f, 25f), ForceMode.Impulse);

			Collider.material = null;
			
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

		public bool IsGrounded()
		{
			for (var i = 0; i < Feet.Length; i++)
			{
				if (!collidingState.Contains(Feet[i]))
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