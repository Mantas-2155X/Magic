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
		public SSlow Slow;
		
		[SerializeField]
		public SParalyze Paralyze;

		[SerializeField]
		public EElement Element;
	}
}