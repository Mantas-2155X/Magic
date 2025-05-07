using System.Collections.Generic;
using AI.Base;
using AI.Enums;
using Combat.Enums;
using Combat.Spells.Base;
using Managers;
using Newtonsoft.Json;
using Objects.Interfaces;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class BaseAliveState
	{
		[JsonProperty]
		public List<string> Wearables;
		
		[JsonProperty]
		public Dictionary<string, BaseSpellState> Spells;
		
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
		
		[JsonProperty]
		public string Grabbing;
		
		[JsonProperty]
		public Vector3? OriginalGrabSize;

		[JsonProperty]
		public bool Alive;
		
		[JsonProperty]
		public bool Invulnerable;

		[JsonProperty]
		public bool Powerful;
		
		public static BaseAliveState Read(BaseAlive baseAlive)
		{
			if (baseAlive == null)
				return null;

			var state = new BaseAliveState();
			
			state.Wearables = new List<string>();
			
			for (var i = 0; i < baseAlive.Wearables.Count; i++)
				state.Wearables.Add(baseAlive.Wearables[i].WearableData.Name);

			state.Spells = new Dictionary<string, BaseSpellState>();
			
			for (var i = 0; i < baseAlive.Spells.Count; i++)
			{
				var spell = baseAlive.Spells[i];
				state.Spells.Add(spell.SpellData.Name, BaseSpellState.Read((BaseSpell)spell));
			}
			
			state.CurrentHealth = baseAlive.CurrentHealth;
			state.CurrentMana = baseAlive.CurrentMana;
			state.CurrentEnergy = baseAlive.CurrentEnergy;
			
			state.MovementType = baseAlive.MovementType;
			state.RelationshipGroup = baseAlive.RelationshipGroup;

			if (baseAlive.Grabbing != null)
			{
				state.Grabbing = baseAlive.Grabbing.ObjectID;
				state.OriginalGrabSize = baseAlive.OriginalGrabSize;
			}
			
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
			
			foreach (var pair in state.Spells)
			{
				var spellState = pair.Value;
			
				var spellData = ObjectManager.Instance.GetSpell(pair.Key);
				baseAlive.LearnSpell(spellData, spellState.Selected);

				var spellIndex = baseAlive.GetSpellIndex(spellData);
				BaseSpellState.Apply((BaseSpell)baseAlive.Spells[spellIndex], spellState);
			}

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
			
			baseAlive.ReleaseObject();
			
			if (!string.IsNullOrEmpty(state.Grabbing))
			{
				var world = World.World.Instance;
				
				var components = world.Objects.GetComponentsInChildren<Component>(true);
				for (var i = 0; i < components.Length; i++)
				{
					var component = components[i];
					if (component is not IObject iObject)
						continue;
					
					if (iObject.ObjectID != state.Grabbing)
						continue;

					baseAlive.GrabObject(iObject);
					
					if (state.OriginalGrabSize != null)
					{
						iObject.GetTransform().localScale = state.OriginalGrabSize.Value;
						iObject.Rigidbody.isKinematic = false;

						baseAlive.ShrinkObject(true);
					}
					break;
				}
			}

			if (!state.Alive && baseAlive.IsAlive)
				baseAlive.Kill(null);

			baseAlive.SetInvulnerable(state.Invulnerable);
			baseAlive.SetPowerful(state.Powerful);
		}
	}
}