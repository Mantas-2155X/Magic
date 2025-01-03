using Combat.Attacks.Enums;
using Combat.Enums;
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
		public bool DropToGround;

		[SerializeField]
		public Vector3 AttachOffset;
		
		[SerializeField]
		public float Damage;

		[SerializeField]
		public EElement Element;
		
		[SerializeField]
		public EAttackAngle AttackAngle;
	}
}