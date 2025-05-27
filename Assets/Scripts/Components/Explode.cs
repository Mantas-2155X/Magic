using UnityEngine;

namespace Components
{
	public class Explode : MonoBehaviour
	{
		[SerializeField]
		public float Force;

		[SerializeField]
		public float Radius;
		
		[SerializeField]
		public Rigidbody[] Rigidbodies;
		
		public Vector3? ExplosionPoint;
		
		public void Awake()
		{
			var position = ExplosionPoint ?? transform.position;
			var scale = transform.localScale;

			Radius *= Mathf.Max(scale.x, scale.y, scale.z);

			for (var i = 0; i < Rigidbodies.Length; i++)
				Rigidbodies[i].AddExplosionForce(Force, position, Radius);
		}
	
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.DrawWireSphere(transform.position, Radius);
		}
#endif
	}
}