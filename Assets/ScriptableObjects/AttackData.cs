using Attacks.Enums;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class AttackData : Data
	{
		[Header("Attack")]
		[SerializeField]
		public float EnableTriggerAfter;
		
		[SerializeField]
		public float DisableTriggerAfter;

		[SerializeField]
		public bool AttachToTarget;

		[SerializeField]
		public EAttackAngle AttackAngle;
	}
}