using System;
using System.Collections.Generic;
using AI.Base;
using AI.Enums;
using Combat.Enums;
using Combat.Spells.Base;
using Combat.Wearables.Base;
using Managers;
using Newtonsoft.Json;
using Objects.Interfaces;
using Tools;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class BaseAliveState
	{
		[JsonProperty]
		public Dictionary<string, BaseWearableState> Wearables;
		
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
		
		// objectid -> amount, remaining duration
		[JsonProperty]
		public Dictionary<string, Tuple<float, float>> SlowSources;
		
		// objectid -> remaining duration
		[JsonProperty]
		public Dictionary<string, float> ParalyzeSources;

		public static BaseAliveState Read(BaseAlive baseAlive)
		{
			if (baseAlive == null)
				return null;

			var state = new BaseAliveState();
			
			state.Wearables = new Dictionary<string, BaseWearableState>();
			
			for (var i = 0; i < baseAlive.Wearables.Count; i++)
			{
				var wearable = baseAlive.Wearables[i];
				state.Wearables.Add(wearable.WearableData.Name, BaseWearableState.Read((BaseWearable)wearable));
			}

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

			if (baseAlive.Grabbing.NotNull())
			{
				state.Grabbing = baseAlive.Grabbing.ObjectID;
				state.OriginalGrabSize = baseAlive.OriginalGrabSize;
			}
			
			state.Alive = baseAlive.IsAlive;
			state.Invulnerable = baseAlive.IsInvulnerable;
			state.Powerful = baseAlive.IsPowerful;

			state.SlowSources = new Dictionary<string, Tuple<float, float>>();
			foreach (var pair in baseAlive.SlowSources)
			{
				Tuple<float, float> tuple;
				
				if (Mathf.Approximately(pair.Value.Item3, float.MaxValue))
					tuple = new Tuple<float, float>(pair.Value.Item1, float.MaxValue);
				else
					tuple = new Tuple<float, float>(pair.Value.Item1, (pair.Value.Item2 + pair.Value.Item3) - Time.time);

				state.SlowSources.Add(pair.Key, tuple);
			}

			state.ParalyzeSources = new Dictionary<string, float>();
			foreach (var pair in baseAlive.ParalyzeSources)
			{
				float duration;
				
				if (Mathf.Approximately(pair.Value.Item2, float.MaxValue))
					duration = float.MaxValue;
				else
					duration = (pair.Value.Item1 + pair.Value.Item2) - Time.time;

				state.ParalyzeSources.Add(pair.Key, duration);
			}
			
			return state;
		}

		public static void Apply(BaseAlive baseAlive, BaseAliveState state)
		{
			if (baseAlive == null)
				return;
			
			baseAlive.RemoveAllWearables();

			foreach (var pair in state.Wearables)
			{
				var wearableState = pair.Value;

				var wearableData = ObjectManager.Instance.GetWearable(pair.Key);
				baseAlive.EquipWearable(wearableData);
				
				var wearableIndex = baseAlive.GetWearableIndex(wearableData);
				BaseWearableState.Apply((BaseWearable)baseAlive.Wearables[wearableIndex], wearableState);
			}
			
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

			foreach (var pair in state.SlowSources)
				baseAlive.AddSlowSource(pair.Key, pair.Value.Item1, pair.Value.Item2);
			
			foreach (var pair in state.ParalyzeSources)
				baseAlive.AddParalyzeSource(pair.Key, pair.Value);
		}
	}
}