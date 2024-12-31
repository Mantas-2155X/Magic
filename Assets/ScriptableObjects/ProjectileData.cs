using Combat.Enums;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class ProjectileData : Data
	{
		[Header("Projectile")]
		[SerializeField]
		public float Force;
		
		[SerializeField]
		public float Damage;

		[SerializeField]
		public EElement Element;

		[SerializeField]
		public AttackData Attack;
	}
}