using Combat.Wearables.Enums;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class WearableData : Data
	{
		[SerializeField]
		public EWearableType WearableType;
	}
}