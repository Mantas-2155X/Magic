using AI.Base;
using AYellowpaper.SerializedCollections;
using Combat.Wearables.Enums;
using Combat.Wearables.Structs;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI
{
	public class Body : MonoBehaviour
	{
		[SerializeField]
		public BaseAlive Alive;
	
		[SerializeField]
		public Rigidbody Rigidbody;

		[SerializeField]
		public Collider BodyCollider;
		
		[SerializeField]
		public Collider FeetCollider;

		[SerializeField]
		public MonoBehaviour[] Gibs;
		
		[SerializeField]
		public Transform[] Shoulders;
		
		[SerializeField]
		public Transform[] Legs;

		[SerializeField]
		public SerializedDictionary<EWearableType, SWearableContainer> Containers;

		[SerializeField]
		public Vector2 SwayAngles = new (30f, 15f);

		[SerializeField]
		public float SwaySpeedMultiplier = 1f;
		
		[SerializeField]
		public float FallMinimumVelocity = 7f;
		
		[SerializeField]
		public float FallDamageMultiplier = 3.5f;
		
		[HideInInspector]
		public bool ShouldSway;
		
		private bool swayDirection;

		public void Update()
		{
			if (!Alive.IsAlive)
				return;
			
			if (ShouldSway)
				swayLimbs();
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
	}
}