using System.Collections.Generic;
using Combat.Attacks.Base;
using Newtonsoft.Json;
using Tools;

namespace State.States
{
	[JsonObject]
	public class BaseAttackState
	{
		[JsonProperty]
		public List<string> TriggeredAlives;
		
		[JsonProperty]
		public List<string> CurrentAlives;
		
		[JsonProperty]
		public List<string> TriggeredObjects;
		
		[JsonProperty]
		public List<string> CurrentObjects;
		
		public static BaseAttackState Read(BaseAttack baseAttack)
		{
			if (baseAttack == null)
				return null;

			var state = new BaseAttackState();
			state.TriggeredAlives = new List<string>();
			state.TriggeredObjects = new List<string>();
			state.CurrentAlives = new List<string>();
			state.CurrentObjects = new List<string>();
			
			for (var i = 0; i < baseAttack.TriggeredAlives.Count; i++)
			{
				var alive = baseAttack.TriggeredAlives[i];
				if (alive.IsNull() || !alive.IsAlive)
					continue;
				
				state.TriggeredAlives.Add(alive.ObjectID);
			}
			
			for (var i = 0; i < baseAttack.TriggeredObjects.Count; i++)
			{
				var obj = baseAttack.TriggeredObjects[i];
				if (obj.IsNull())
					continue;
				
				state.TriggeredObjects.Add(obj.ObjectID);
			}
			
			for (var i = 0; i < baseAttack.CurrentAlives.Count; i++)
			{
				var alive = baseAttack.CurrentAlives[i];
				if (alive.IsNull() || !alive.IsAlive)
					continue;
				
				state.CurrentAlives.Add(alive.ObjectID);
			}
			
			for (var i = 0; i < baseAttack.CurrentObjects.Count; i++)
			{
				var obj = baseAttack.CurrentObjects[i];
				if (obj.IsNull())
					continue;
				
				state.CurrentObjects.Add(obj.ObjectID);
			}
			
			return state;
		}

		public static void Apply(BaseAttack baseAttack, BaseAttackState state)
		{
			if (baseAttack == null)
				return;

			baseAttack.SetState(state.TriggeredAlives, state.TriggeredObjects, state.CurrentAlives, state.CurrentObjects);
		}
	}
}