using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class PlayerData : AliveData
	{
		[Header("Player")]
		[SerializeField]
		public float MovementForce = 1f;
		
		[SerializeField]
		public float JumpForce = 115f;

		[SerializeField]
		public float SprintMultiplier = 1.25f;
		
		[SerializeField]
		public float StopSlide = 0.65f;
		
		[SerializeField]
		public float AirMovement = 0.1f;

		[SerializeField]
		public float SpeedClampModifier = 0.91f;

		[SerializeField]
		public float SprintEnergy = 15f;
	}
}