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
		public bool IsResource;
		
		[SerializeField]
		public CastData Cast;

		[SerializeField]
		public ProjectileData Projectile;
		
		[SerializeField]
		public AttackData Attack;
	}
}