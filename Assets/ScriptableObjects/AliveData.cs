using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Combat.Enums;
using Combat.Structs;
using UnityEngine;

namespace ScriptableObjects
{
	public class AliveData : Data
	{
		[Header("Base Data")]
		[SerializeField]
		public float Health;
		
		[SerializeField]
		public float Mana;

		[SerializeField]
		public float Energy;

		[SerializeField]
		public float RegenerateHealth;
		
		[SerializeField]
		public float RegenerateMana;

		[SerializeField]
		public float RegenerateEnergy;

		[SerializeField]
		public float Speed;
		
		[Header("Base Stats")]
		[SerializeField]
		public SerializedDictionary<EElement, SElementStat> DamageStats;
		
		[SerializeField]
		public SerializedDictionary<EElement, SElementStat> ProtectionStats;

		/// <summary>
		/// First spell is classed as primary, NPCs will prefer using it
		/// </summary>
		[Header("Base Spells")]
		[SerializeField]
		public List<SpellData> Spells;
		
		[Header("Base Wearables")]
		[SerializeField]
		public List<WearableData> Wearables;
		
		[Header("Broken")]
		[SerializeField]
		public GameObject BrokenBodyPrefab;
		
		[SerializeField]
		public GameObject BrokenArmPrefab;

		[SerializeField]
		public GameObject BrokenFootPrefab;
	}
}