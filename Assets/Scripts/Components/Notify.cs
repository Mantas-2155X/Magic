using UI;
using UI.Enums;
using UnityEngine;

namespace Components
{
	public class Notify : MonoBehaviour
	{
		[SerializeField]
		public ENoticePresetFlags Preset;

		[SerializeField]
		public float Duration = 4f;
		
		public void Trigger()
		{
			var player = Player.Instance;
			if (player == null)
				return;
			
			player.Notice.ShowMessage(Preset, Duration);
		}
	}
}