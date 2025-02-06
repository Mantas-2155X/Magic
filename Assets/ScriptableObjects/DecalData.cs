using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class DecalData : Data
	{
		[Header("Projector")]
		[SerializeField]
		public float Size = 0.25f;

		[Header("Fade")]
		[SerializeField]
		public float FadeAfter = 5f;
		
		[SerializeField]
		public float FadeDuration = 5f;
	}
}