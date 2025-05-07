using AI;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class PlayerState
	{
		[JsonProperty]
		public Vector3 CameraAngles;

		public static PlayerState Read(Player player)
		{
			if (player == null)
				return null;

			return new PlayerState
			{
				CameraAngles = player.CameraTr.eulerAngles,
			};
		}

		public static void Apply(Player player, PlayerState state)
		{
			if (player == null)
				return;

			player.CameraTr.eulerAngles = state.CameraAngles;
		}
	}
}