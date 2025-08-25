using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Combat.Enums;
using Combat.Structs;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
		public AssetReference BrokenBodyPrefabReference;
		
		[SerializeField]
		public AssetReference BrokenArmPrefabReference;

		[SerializeField]
		public AssetReference BrokenFootPrefabReference;
		
		[SerializeField]
		public AssetReference BreakAudioReference;
		
		[Header("Grab")]
		[SerializeField]
		public bool CanGrab = true;

		[SerializeField]
		public float GrabEnergy = 5f;
		
		[SerializeField]
		public float GrabShinkedEnergy = 7.5f;

		[SerializeField]
		public float GrabPositionSpeed = 15f;

		[SerializeField]
		public float GrabRotationSpeed = 15f;

		[SerializeField]
		public float GrabVerticalOffset = -0.15f;
		
		[SerializeField]
		public float GrabMass = 25f;

		[SerializeField]
		public float GrabDropDistance = 5f;

		[SerializeField]
		public float GrabDropAngle = 100f;

		[Header("Impact")]
		[SerializeField]
		public float ImpactMinimumThreshold = 1800;

		[SerializeField]
		public float ImpactDamageScale = 0.0115f;
		
		[Header("Other")]
		public bool AttachDecals = true;
	}
}