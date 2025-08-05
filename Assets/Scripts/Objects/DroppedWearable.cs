using System;
using AI.Interfaces;
using Combat.Wearables.Interfaces;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Base;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using UnityEngine;

namespace Objects
{
	public class DroppedWearable : BaseObject
	{
		public IWearable Wearable;

		#region Identify / SaveLoad

		public override ELoadType LoadType => ELoadType.Create;
		
		public override ELoadTiming LoadTiming => ELoadTiming.Normal;

		public override JObject GetCreation()
		{
			var createData = new CreateData()
			{
				Name = Wearable.WearableData.Name, // Actual wearable name here instead of droppedwearable
				States = GetModifications()
			};

			return JObject.FromObject(createData);
		}
		
		public new static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<CreateData>();
			
			var wearable = ObjectManager.Instance.CreateWearable(ObjectManager.Instance.GetData<WearableData>(createData.Name), Vector3.zero, Vector3.zero);
			wearable.ObjectID = data.Item1;
			wearable.Drop();

			var obj = wearable.GetGameObject().GetComponent<DroppedWearable>();

			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[DroppedWearable] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		#endregion
		
		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && !user.HasWearable(Wearable.WearableData);
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			user.EquipWearable(Wearable);
			return true;
		}
	}
}