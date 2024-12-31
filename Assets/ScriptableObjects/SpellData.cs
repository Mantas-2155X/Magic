using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class SpellData : Data
	{
		[Header("Spell")]
		[SerializeField]
		public float Cooldown;
		
		[SerializeField]
		public float CastingTime;
		
		[SerializeField]
		public float CastingCost;

		[SerializeField]
		public float Range;
		
		[SerializeField]
		public CastData Cast;

		[SerializeField]
		public AttackData Attack;

		[SerializeField]
		public ProjectileData Projectile;
	}
}