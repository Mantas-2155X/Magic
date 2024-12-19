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
		public float Range;
		
		[SerializeField]
		public float Damage;

		[SerializeField]
		public AttackData Attack;
	}
}