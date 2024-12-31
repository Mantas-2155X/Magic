using AYellowpaper.SerializedCollections;
using Combat.Enums;
using Combat.Structs;
using Combat.Wearables.Enums;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class WearableData : Data
	{
		[SerializeField]
		public EWearableType WearableType;

		[SerializeField]
		[SerializedDictionary("Element", "Damage Modifier")]
		public SerializedDictionary<EElement, SElementStat> DamageStats;
		
		[SerializeField]
		[SerializedDictionary("Element", "Protection Modifier")]
		public SerializedDictionary<EElement, SElementStat> ProtectionStats;
	}
}