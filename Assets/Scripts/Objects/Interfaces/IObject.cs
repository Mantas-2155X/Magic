using System;
using System.Collections.Generic;
using AI.Interfaces;
using Combat.Enums;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IObject
	{
		public ObjectData ObjectData { get; set; }

		public string ObjectID { get; set; }
		
		#region Breakable

		public float Health { get; }

		public void Damage(float damage, object source, EElement type);
		public void Break(object source);

		#endregion

		#region Pickupable

		public bool Pickupable { get; }

		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		#endregion

		#region Usable

		public bool Usable { get; }

		public bool CanUse(IAlive user);
		public bool Use(IAlive user);
		
		#endregion
		
		public void Spawn(Vector3 position, Vector3 angles);

		public Dictionary<string, JObject> Save();
		
		public void Load(Dictionary<string, JObject> data);
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}