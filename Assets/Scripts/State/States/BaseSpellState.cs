using Combat.Spells.Base;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class BaseSpellState
	{
		[JsonProperty]
		public string ObjectID;
		
		[JsonProperty]
		public bool Selected;

		[JsonProperty]
		public float Cooldown;
		
		public static BaseSpellState Read(BaseSpell baseSpell)
		{
			if (baseSpell == null)
				return null;

			var state = new BaseSpellState
			{
				ObjectID = baseSpell.ObjectID,
				Selected = baseSpell.IsSelected
			};

			if (baseSpell.IsOnCooldown)
			{
				state.Cooldown = (baseSpell.LastFinishedCast + baseSpell.SpellData.Cooldown) - Time.time;
			}
			else
			{
				state.Cooldown = 0f;
			}
			
			return state;
		}

		public static void Apply(BaseSpell baseSpell, BaseSpellState state)
		{
			if (baseSpell == null)
				return;

			baseSpell.ObjectID = state.ObjectID;
			baseSpell.SetState(state.Cooldown);
		}
	}
}