using Objects.Base;
using Objects.Interfaces;
using ScriptableObjects.Enums;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace AI.ActionModes.Shared
{
	public class LowResources
	{
		private readonly NPC owner;

		public LowResources(NPC owner)
		{
			this.owner = owner;
		}

		// todo: optimize this too, hell optimize this whole class
		public IObject GrabResource()
		{
			if (IsLowHealth())
			{
				var currentOtherTarget = owner.OtherTarget;
				if (currentOtherTarget != null && currentOtherTarget is IObject obj && obj.ObjectData.Tags.HasFlag(ETag.RestoresHealth) && owner.WithinRange.SenseDistanceCheck(obj.GetTransform()))
					return obj;

				return IsObjectNearby(ETag.RestoresHealth);
			}
			
			if (IsLowMana())
			{
				var currentOtherTarget = owner.OtherTarget;
				if (currentOtherTarget != null && currentOtherTarget is IObject obj && obj.ObjectData.Tags.HasFlag(ETag.RestoresMana) && owner.WithinRange.SenseDistanceCheck(obj.GetTransform()))
					return obj;

				return IsObjectNearby(ETag.RestoresMana);
			}

			return null;
		}
		
		// todo: make as a property in the npc class. Probably what's the minimum before this is true
		public bool IsLowHealth()
		{
			return owner.CurrentHealth <= owner.StartingHealth / 2f;
		}

		// todo: make as a property in the npc class. Probably what's the minimum before this is true
		public bool IsLowMana()
		{
			return owner.CurrentMana <= owner.StartingMana / 2f;
		}

		// todo: optimize this to hell, extremely inefficient. Only testing. Probably cache the objects in ObjectManager
		public IObject IsObjectNearby(ETag tag)
		{
			var objects = Object.FindObjectsByType<BaseObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (var obj in objects)
			{
				if (!obj.enabled || !obj.ObjectData.Tags.HasFlag(tag))
					continue;

				var tr = obj.GetTransform();
				
				if (!owner.WithinRange.SenseDistanceCheck(tr))
					continue;

				var path = new NavMeshPath();

				var prevAgent = owner.Agent.enabled;

				owner.Agent.enabled = true;
				var calc = owner.Agent.CalculatePath(tr.position, path);
				owner.Agent.enabled = prevAgent;
				
				if (!calc || path.status is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathPartial)
					continue;

				return obj;
			}

			return null;
		}
	}
}