using Newtonsoft.Json;
using Objects.Base;

namespace State.States
{
	[JsonObject]
	public class BaseObjectState
	{
		[JsonProperty]
		public float Health;

		[JsonProperty]
		public bool Pickupable;

		[JsonProperty]
		public bool Usable;
				
		public static BaseObjectState Read(BaseObject baseObject)
		{
			if (baseObject == null)
				return null;

			return new BaseObjectState
			{
				Health = baseObject.Health,
				Pickupable = baseObject.Pickupable,
				Usable = baseObject.Usable
			};
		}

		public static void Apply(BaseObject baseObject, BaseObjectState state)
		{
			if (baseObject == null)
				return;

			baseObject.Health = state.Health;
			baseObject.Pickupable = state.Pickupable;
			baseObject.Usable = state.Usable;
		}
	}
}