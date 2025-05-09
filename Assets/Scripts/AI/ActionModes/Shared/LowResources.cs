using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using ScriptableObjects.Enums;
using UnityEngine;
using UnityEngine.AI;

namespace AI.ActionModes.Shared
{
	public class LowResources
	{
		private readonly NPC owner;

		public LowResources(NPC owner)
		{
			this.owner = owner;
		}

		private readonly List<IObject> tempResources = new ();
		private readonly List<Vector3> tempResourcePositions = new ();

		public bool UseResourceSpellIfNeeded()
		{
			if (owner.Paralyzed)
				return false;

			var spells = owner.Spells;
			
			if (IsHalfHealth())
			{
				for (var i = 0; i < spells.Count; i++)
				{
					var spell = spells[i];
					
					var spellData = spell.SpellData;
					if (!spellData.IsResource || !spell.CanCast())
						continue;
					
					if (spellData.Tags.HasFlag(ETag.RestoresHealth))
					{
						owner.SelectSpell(spellData);
						owner.SwitchCastCooldown = 0f;
						owner.Spell.StartCasting();

						return true;
					}
				}
			}

			if (IsHalfMana())
			{
				for (var i = 0; i < spells.Count; i++)
				{
					var spell = spells[i];

					var spellData = spell.SpellData;
					if (!spellData.IsResource || !spell.CanCast())
						continue;
					
					if (spellData.Tags.HasFlag(ETag.RestoresMana))
					{
						owner.SelectSpell(spellData);
						owner.SwitchCastCooldown = 0f;
						owner.Spell.StartCasting();

						return true;
					}
				}
			}
			
			if (IsHalfEnergy())
			{
				for (var i = 0; i < spells.Count; i++)
				{
					var spell = spells[i];

					var spellData = spell.SpellData;
					if (!spellData.IsResource || !spell.CanCast())
						continue;
					
					if (spellData.Tags.HasFlag(ETag.RestoresEnergy))
					{
						owner.SelectSpell(spellData);
						owner.SwitchCastCooldown = 0f;
						owner.Spell.StartCasting();

						return true;
					}
				}
			}

			return false;
		}
		
		public bool GrabResourceIfNeeded()
		{
			if (owner.Paralyzed || !((NPCData)owner.Data).CanTakeResources)
				return false;

			if (IsLowHealth())
			{
				if (CurrentResourceValid(ETag.RestoresHealth))
				{
					owner.Use(owner.OtherTarget);
					return true;
				}

				var resource = FindNearbyResource(ETag.RestoresHealth);
				if (resource != null)
				{
					owner.Use((Component)resource);
					return true;
				}
			}
			
			if (IsLowMana())
			{
				if (CurrentResourceValid(ETag.RestoresMana))
				{
					owner.Use(owner.OtherTarget);
					return true;
				}
				
				var resource = FindNearbyResource(ETag.RestoresMana);
				if (resource != null)
				{
					owner.Use((Component)resource);
					return true;
				}
			}
			
			if (IsLowEnergy())
			{
				if (CurrentResourceValid(ETag.RestoresEnergy))
				{
					owner.Use(owner.OtherTarget);
					return true;
				}
				
				var resource = FindNearbyResource(ETag.RestoresEnergy);
				if (resource != null)
				{
					owner.Use((Component)resource);
					return true;
				}
			}

			return false;
		}
		
		public IObject FindNearbyResource(ETag tag)
		{
			tempResources.Clear();
			tempResourcePositions.Clear();

			var resources = StateManager.Instance.RegisteredObjects;
			foreach (var pair in resources)
			{
				if (pair.Value == null || pair.Value is not IObject resource)
					continue;

				// Don't grab disabled ones
				if (resource is MonoBehaviour mono && !mono.enabled)
					continue;
				
				// Make sure it's a resource
				if (!resource.ObjectData.Tags.HasFlag(tag))
					continue;

				var resourceTr = resource.GetTransform();
				var resourcePos = resourceTr.position;
				
				if (!owner.WithinRange.SenseDistanceCheck(resourceTr, false, false))
					continue;

				// Prevent picking a destination that's behind a wall
				if (NavMesh.Raycast(owner.GetTransform().position, resourcePos, out _, NavMesh.AllAreas))
					continue;
				
				tempResources.Add(resource);
				tempResourcePositions.Add(resourcePos);
			}

			if (tempResources.Count == 0)
				return null;
			
			if (tempResources.Count == 1)
				return tempResources[0];

			var closestResource = -1;
			var closestDistance = float.MaxValue;

			for (var i = 0; i < tempResourcePositions.Count; i++)
			{
				var thisPos = tempResourcePositions[i];
				
				for (var k = 0; k < tempResourcePositions.Count; k++)
				{
					if (i == k)
						continue;

					var otherPos = tempResourcePositions[k];
					var distance = Vector3.Distance(thisPos, otherPos);
					
					if (distance < closestDistance)
					{
						closestResource = i;
						closestDistance = distance;
					}
				}
			}

			if (closestResource == -1)
				return null;
			
			return tempResources[closestResource];
		}

		public bool CurrentResourceValid(ETag tag)
		{
			var resource = owner.OtherTarget;
			if (resource == null || resource is not IObject obj)
				return false;

			if (!obj.ObjectData.Tags.HasFlag(tag))
				return false;

			if (!owner.WithinRange.SenseDistanceCheck(owner.OtherTargetTransform, false, false))
				return false;
				
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsLowHealth() => owner.CurrentHealth <= owner.Data.Health * ((NPCData)owner.Data).LowResourcesMultiplier;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsLowMana() => owner.CurrentMana <= owner.Data.Mana * ((NPCData)owner.Data).LowResourcesMultiplier;
		public bool IsLowEnergy() => owner.CurrentEnergy <= owner.Data.Energy * ((NPCData)owner.Data).LowResourcesMultiplier;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsHalfHealth() => owner.CurrentHealth <= owner.Data.Health / 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsHalfMana() => owner.CurrentMana <= owner.Data.Mana / 2f;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsHalfEnergy() => owner.CurrentEnergy <= owner.Data.Energy / 2f;

	}
}