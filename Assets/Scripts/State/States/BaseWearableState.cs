using Combat.Wearables.Base;
using Newtonsoft.Json;

namespace State.States
{
	[JsonObject]
	public class BaseWearableState
	{
		[JsonProperty]
		public string ObjectID;
		
		public static BaseWearableState Read(BaseWearable baseWearable)
		{
			if (baseWearable == null)
				return null;

			return new BaseWearableState
			{
				ObjectID = baseWearable.ObjectID,
			};
		}

		public static void Apply(BaseWearable baseWearable, BaseWearableState state)
		{
			if (baseWearable == null)
				return;

			baseWearable.ObjectID = state.ObjectID;
		}
	}
}