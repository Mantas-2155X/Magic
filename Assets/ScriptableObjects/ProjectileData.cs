using Combat.Enums;
using ScriptableObjects.Structs;
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
		public int Bounces;

		[SerializeField]
		public SSlow Slow;
		
		[SerializeField]
		public SParalyze Paralyze;

		[SerializeField]
		public EElement Element;
		
		[SerializeField]
		public DecalData Decal;

		[SerializeField]
		public bool ImpactSound = true;
	}
}