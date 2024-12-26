using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Managers;
using Objects.Interfaces;
using ScriptableObjects.Enums;
using Unity.Mathematics;
using UnityEngine;

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
		
		public bool GrabResourceIfNeeded()
		{
			if (IsLowHealth())
			{
				if (CurrentResourceValid(ETag.RestoresHealth))
				{
					owner.UseSomething(owner.OtherTarget);
					return true;
				}

				var resource = FindNearbyResource(ETag.RestoresHealth);
				if (resource != null)
				{
					owner.UseSomething((Component)resource);
					return true;
				}
			}
			
			if (IsLowMana())
			{
				if (CurrentResourceValid(ETag.RestoresMana))
				{
					owner.UseSomething(owner.OtherTarget);
					return true;
				}
				
				var resource = FindNearbyResource(ETag.RestoresMana);
				if (resource != null)
				{
					owner.UseSomething((Component)resource);
					return true;
				}
			}

			return false;
		}
		
		public IObject FindNearbyResource(ETag tag)
		{
			tempResources.Clear();
			tempResourcePositions.Clear();

			var resources = ObjectManager.Instance.GetRegisteredObjects();
			for (var i = 0; i < resources.Count; i++)
			{
				var resource = resources[i];
				if (resource == null)
					continue;

				if (!resource.ObjectData.Tags.HasFlag(tag))
					continue;

				var resourceTr = resource.GetTransform();
				var resourcePos = resourceTr.position;
				
				if (!owner.WithinRange.SenseDistanceCheck(resourceTr) || !owner.WithinRange.IsPathValid(resourcePos))
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
					var distance = math.distancesq(thisPos, otherPos);
					
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

			if (!owner.WithinRange.SenseDistanceCheck(owner.OtherTargetTransform))
				return false;
				
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsLowHealth() => owner.CurrentHealth <= owner.StartingHealth * owner.LowResourcesMultiplier;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsLowMana() => owner.CurrentMana <= owner.StartingMana * owner.LowResourcesMultiplier;
	}
}