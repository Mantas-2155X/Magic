using Combat.Attacks.Enums;
using Combat.Enums;
using ScriptableObjects.Structs;
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
		public bool IgnoreCaster;

		[SerializeField]
		public bool FollowCaster;
		
		[SerializeField]
		public Vector3 AttachOffset;
		
		[SerializeField]
		public float Damage;

		[SerializeField]
		public SSlow Slow;

		[SerializeField]
		public SParalyze Paralyze;
		
		[SerializeField]
		public EElement Element;
		
		[SerializeField]
		public EAttackAngle AttackAngle;
		
		[SerializeField]
		public EAttackOrigin AttackOrigin;
	}
}