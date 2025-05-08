using AI;
using Newtonsoft.Json;
using UI.Enums;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class PlayerState
	{
		[JsonProperty]
		public Vector3 CameraAngles;

		[JsonProperty]
		public ENoticePresetFlags? NoticePreset;
		
		[JsonProperty]
		public float NoticeDuration;
		
		public static PlayerState Read(Player player)
		{
			if (player == null)
				return null;

			var state = new PlayerState
			{
				CameraAngles = player.CameraTr.eulerAngles,
			};

			var playerUI = UI.Player.Instance;
			if (playerUI != null && playerUI.Notice.isActiveAndEnabled)
			{
				state.NoticePreset = playerUI.Notice.CurrentPreset;
				state.NoticeDuration = playerUI.Notice.EndTime - Time.time;
			}
			
			return state;
		}

		public static void Apply(Player player, PlayerState state)
		{
			if (player == null)
				return;

			player.CameraTr.eulerAngles = state.CameraAngles;
			
			var playerUI = UI.Player.Instance;
			if (playerUI != null && state.NoticePreset != null)
			{
				playerUI.Notice.ShowMessage(state.NoticePreset.Value, state.NoticeDuration);
			}
		}
	}
}