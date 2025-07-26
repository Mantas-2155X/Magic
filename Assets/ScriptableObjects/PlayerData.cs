using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class PlayerData : AliveData
	{
		[Header("Player")]
		[SerializeField]
		public float SprintMultiplier = 1.25f;
		
		[SerializeField]
		public float NoclipSpeed = 10f;

		[SerializeField]
		public float Acceleration = 1.75f;
		
		[SerializeField]
		public float AirAcceleration = 0.15f;

		[SerializeField]
		public float Friction = 750f;

		[SerializeField]
		public float JumpForce = 125f;

		[SerializeField]
		public float SprintEnergy = 15f;

		[SerializeField]
		public float JumpEnergy = 3f;
	}
}