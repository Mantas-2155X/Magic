using UnityEngine;

public class Explode : MonoBehaviour
{
	[SerializeField]
	public float Force;

	[SerializeField]
	public float Radius;
		
	[SerializeField]
	public Rigidbody[] Rigidbodies;
		
	public void Awake()
	{
		var position = transform.position;

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