using System;
using AI.Interfaces;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Enums;
using State;
using State.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseGib : BaseObject
	{
		[SerializeField]
		public EGibType Type;
		
		[SerializeField]
		public float Amount;
		
		#region Identify / SaveLoad

		public new static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<CreateData>();
			
			var obj = (BaseObject)ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject(createData.Name), Vector3.zero, Vector3.zero);
			obj.ObjectID = data.Item1;

			var tr = obj.GetTransform();
			tr.SetParent(World.World.Instance.Ragdolls);

			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[BaseGib] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		#endregion
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			switch (Type)
			{
				case EGibType.Health:
					user.RestoreHealth(Amount, this);
					break;
				case EGibType.Mana:
					user.RestoreMana(Amount, this);
					break;
				case EGibType.Energy:
					user.RestoreEnergy(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
			
			return true;
		}
	}
}