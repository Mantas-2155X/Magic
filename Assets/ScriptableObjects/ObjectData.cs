using Objects.Enums;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class ObjectData : Data
	{
		[Header("Object Pool")]
		public EObjectPool IsPoolable;
		
		[Header("Breakable")]
		[SerializeField]
		public bool IsBreakable;

		[SerializeField]
		public bool BreakAtCollisionPoint;
		
		[SerializeField]
		public float MaximumHealth;

		[SerializeField]
		public GameObject BrokenPrefab;
		
		[Header("Pickupable")]
		[SerializeField]
		public bool IsPickupable;

		[SerializeField]
		public float PickupableAfter;
		
		[SerializeField]
		public EAction PickupAction;

		[Header("Usable")]
		[SerializeField]
		public bool IsUsable;
		
		[SerializeField]
		public float UsableAfter;

		[SerializeField]
		public EAction UseAction;

		[Header("Other")]
		public bool AttachDecals = true;
	}
}