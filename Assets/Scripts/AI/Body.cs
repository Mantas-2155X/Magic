using System;
using AI.Base;
using AYellowpaper.SerializedCollections;
using Combat.Enums;
using Combat.Wearables.Enums;
using Combat.Wearables.Structs;
using Managers;
using Objects.Base;
using UnityEngine;

namespace AI
{
	public class Body : MonoBehaviour
	{
		[SerializeField]
		public BaseAlive Alive;
	
		[SerializeField]
		public Rigidbody Rigidbody;

		[SerializeField]
		public Collider HitboxCollider;
		
		[SerializeField]
		public Collider MovementCollider;

		[SerializeField]
		public BaseGib[] Gibs;
		
		[SerializeField]
		public Transform[] Shoulders;
		
		[SerializeField]
		public Transform[] Legs;

		[SerializeField]
		public Transform[] Arms;

		[SerializeField]
		public Transform[] Feet;
		
		[SerializeField]
		public Transform Core;
		
		[SerializeField]
		public SerializedDictionary<EWearableType, SWearableContainer> Containers;

		[SerializeField]
		public SerializedDictionary<EElement, CoreColor> CoreColors;
		
		[SerializeField]
		public Vector2 SwayAngles = new (30f, 15f);

		[SerializeField]
		public float SwaySpeedMultiplier = 1f;
		
		[SerializeField]
		public float FallMinimumVelocity = 7f;
		
		[SerializeField]
		public float FallDamageMultiplier = 3.5f;
		
		[SerializeField]
		public ParticleSystem Malfunction;

		[SerializeField]
		public bool CanSway = true;
		
		[HideInInspector]
		public bool ShouldSway;
		
		private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

		private Transform movementColliderTr;
		private bool hasMovementCollider;

		private bool swayDirection;

		private EElement coreGlowElement;
		private bool coreCenterActive;
		
		private bool malfunctionActive;

		private Material[] instancedMaterials;

		public void Awake()
		{
			instancedMaterials = Core.GetComponent<Renderer>().materials;
			
			if (MovementCollider != null)
			{
				hasMovementCollider = true;
				movementColliderTr = MovementCollider.transform;
			}
		}
		
		public void OnDestroy()
		{
			if (instancedMaterials == null)
				return;

			for (var i = 0; i < instancedMaterials.Length; i++)
			{
				Destroy(instancedMaterials[i]);
				instancedMaterials[i] = null;
			}
		}

		public void SetCoreGlow(EElement element)
		{
			if (coreGlowElement == element)
				return;
			
			coreGlowElement = element;
			
			var coreColor = CoreColors[element];
			var glowMaterial = instancedMaterials[1];
				
			glowMaterial.color = coreColor.Color;
			glowMaterial.SetColor(emissionColor, coreColor.EmissionColor);
		}
		
		public void SetCoreCenter(bool active)
		{
			if (coreCenterActive == active)
				return;

			coreCenterActive = active;
			
			var centerMaterial = instancedMaterials[2];
			
			if (active)
			{
				var color = Color.white;
				centerMaterial.color = color;
				centerMaterial.SetColor(emissionColor, color * 1.25f);
			}
			else
			{
				var color = Color.black;
				centerMaterial.color = color;
				centerMaterial.SetColor(emissionColor, color);
			}
		}

		public void SetMalfunction(bool active)
		{
			if (malfunctionActive == active)
				return;
			
			malfunctionActive = active;
			
			if (active)
				Malfunction.Play(true);
			else
				Malfunction.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (!Alive.IsAlive)
				return;
			
			if (hasMovementCollider)
				movementColliderTr.rotation = Quaternion.identity;

			if (ShouldSway && CanSway)
				swayLimbs();
		}

		public void LateUpdate()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (!Alive.IsAlive)
				return;

			if (hasMovementCollider)
				movementColliderTr.rotation = Quaternion.identity;
		}
		
		public void FixedUpdate()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (!Alive.IsAlive)
				return;

			if (hasMovementCollider)
				movementColliderTr.rotation = Quaternion.identity;
		}

		private void swayLimbs()
		{
			var incrementAmount = Alive.CurrentSpeed * SwaySpeedMultiplier * Time.deltaTime;
			
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

			if (!Alive.IsWalking && Mathf.Abs(currentAngle) < 1.5f)
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

		[Serializable]
		public struct CoreColor
		{
			[ColorUsage(false, false)]
			public Color Color;
			
			[ColorUsage(false, true)]
			public Color EmissionColor;
		}
	}
}