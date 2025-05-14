using System.Collections.Generic;
using AI.Navigation;
using Newtonsoft.Json;
using Tools;

namespace State.States
{
	[JsonObject]
	public class NavMeshDoorLinkState
	{
		[JsonProperty]
		public List<string> LinkUsersObjectIDs;

		[JsonProperty]
		public string UserObjectID;

		[JsonProperty]
		public bool Partial;
		
		public static NavMeshDoorLinkState Read(NavMeshDoorLink navMeshDoorLink)
		{
			if (navMeshDoorLink == null)
				return null;

			var state = new NavMeshDoorLinkState
			{
				LinkUsersObjectIDs = new List<string>(),
				UserObjectID = navMeshDoorLink.User != null ? navMeshDoorLink.User.ObjectID : null,
				Partial = navMeshDoorLink.IsPartial
			};

			for (var i = 0; i < navMeshDoorLink.LinkUsers.Count; i++)
			{
				var npc = navMeshDoorLink.LinkUsers[i];
				if (npc.IsNull() || !npc.IsAlive)
					continue;

				state.LinkUsersObjectIDs.Add(npc.ObjectID);
			}

			return state;
		}

		public static void Apply(NavMeshDoorLink navMeshDoorLink, NavMeshDoorLinkState state)
		{
			if (navMeshDoorLink == null)
				return;

			navMeshDoorLink.SetState(state.LinkUsersObjectIDs, state.UserObjectID, state.Partial);
		}
	}
}