using System.Collections.Generic;
using AI.Base;
using AI.Enums;
using Combat.Enums;
using Managers;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class BaseAliveState
	{
		[JsonProperty]
		public List<string> Wearables;
		
		[JsonProperty]
		public List<string> Spells;
		
		[JsonProperty]
		public string Spell;
		
		[JsonProperty]
		public float CurrentHealth;
		
		[JsonProperty]
		public float CurrentMana;
		
		[JsonProperty]
		public float CurrentEnergy;
		
		[JsonProperty]
		public EMovementType MovementType;

		[JsonProperty]
		public int RelationshipGroup;
		
		// slow
		
		// paralyze
		
		// grabbing

		[JsonProperty]
		public bool Alive;
		
		[JsonProperty]
		public bool Invulnerable;

		[JsonProperty]
		public bool Powerful;
		
		// spell casting
		
		// spell cooldowns

		// spell override range
		
		public static BaseAliveState Read(BaseAlive baseAlive)
		{
			if (baseAlive == null)
				return null;

			var state = new BaseAliveState();
			
			state.Wearables = new List<string>();
			
			for (var i = 0; i < baseAlive.Wearables.Count; i++)
				state.Wearables.Add(baseAlive.Wearables[i].WearableData.Name);

			state.Spells = new List<string>();
			
			for (var i = 0; i < baseAlive.Spells.Count; i++)
				state.Spells.Add(baseAlive.Spells[i].SpellData.Name);

			state.Spell = baseAlive.Spell.SpellData.Name;
			
			state.CurrentHealth = baseAlive.CurrentHealth;
			state.CurrentMana = baseAlive.CurrentMana;
			state.CurrentEnergy = baseAlive.CurrentEnergy;
			
			state.MovementType = baseAlive.MovementType;
			state.RelationshipGroup = baseAlive.RelationshipGroup;

			state.Alive = baseAlive.IsAlive;
			state.Invulnerable = baseAlive.IsInvulnerable;
			state.Powerful = baseAlive.IsPowerful;
			
			return state;
		}

		public static void Apply(BaseAlive baseAlive, BaseAliveState state)
		{
			if (baseAlive == null)
				return;
			
			baseAlive.RemoveAllWearables();

			for (var i = 0; i < state.Wearables.Count; i++)
				baseAlive.EquipWearable(ObjectManager.Instance.GetWearable(state.Wearables[i]));
			
			baseAlive.ForgetAllSpells();
			
			for (var i = 0; i < state.Spells.Count; i++)
				baseAlive.LearnSpell(ObjectManager.Instance.GetSpell(state.Spells[i]), false);
			
			baseAlive.SelectSpell(ObjectManager.Instance.GetSpell(state.Spell));

			var addHealth = state.CurrentHealth - baseAlive.CurrentHealth;
			switch (addHealth)
			{
				case > 0:
					baseAlive.RestoreHealth(addHealth, null);
					break;
				case < 0:
					baseAlive.Damage(Mathf.Abs(addHealth), null, EElement.Unknown);
					break;
			}
			
			var addMana = state.CurrentMana - baseAlive.CurrentMana;
			switch (addMana)
			{
				case > 0:
					baseAlive.RestoreMana(addMana, null);
					break;
				case < 0:
					baseAlive.TakeMana(Mathf.Abs(addMana), null);
					break;
			}
			
			var addEnergy = state.CurrentEnergy - baseAlive.CurrentEnergy;
			switch (addEnergy)
			{
				case > 0:
					baseAlive.RestoreEnergy(addEnergy, null);
					break;
				case < 0:
					baseAlive.TakeEnergy(Mathf.Abs(addEnergy), null);
					break;
			}
			
			baseAlive.SetMovementType(state.MovementType);
			baseAlive.SetRelationshipGroup(state.RelationshipGroup);
			
			if (!state.Alive && baseAlive.IsAlive)
				baseAlive.Kill(null);

			baseAlive.SetInvulnerable(state.Invulnerable);
			baseAlive.SetPowerful(state.Powerful);
		}
	}
}