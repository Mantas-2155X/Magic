using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class WeaponData : Data
	{
		[SerializeField]
		public float ManaCost;
		
		[SerializeField]
		public float Cooldown;
		
		[SerializeField]
		public float CastingTime;
		
		[SerializeField]
		public float MaximumDistance;
		
		[SerializeField]
		public CastData Cast;

		[SerializeField]
		public AttackData Attack;

		[SerializeField]
		public ProjectileData Projectile;
	}
}