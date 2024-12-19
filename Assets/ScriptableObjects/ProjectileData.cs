using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class ProjectileData : Data
	{
		[SerializeField]
		public float Range;
		
		[SerializeField]
		public float Damage;
	}
}