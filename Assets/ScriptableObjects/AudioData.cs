using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class AudioData : Data
	{
		[Header("Audio Settings")]
		[SerializeField]
		public AssetReference[] ClipReferences;

		[SerializeField][Range(0f, 1f)]
		public float Volume = 1f;
		
		[SerializeField]
		public float Distance = 25f;

		[SerializeField]
		public bool Loop;

		[Header("Spatialization")]
		[SerializeField][Range(0f, 1f)]
		public float Spatialize = 1f;

		[SerializeField][Space(5f)]
		public bool DistanceAttenuation = true;

		[SerializeField]
		public bool AirAbsorption = true;
		
		[SerializeField]
		public bool Occlusion = true;

		[SerializeField][Space(5f)]
		public bool Transmission = true;

		[SerializeField][Range(1, 8)]
		public int TransmissionSurfaces = 4;
		
		[SerializeField][Range(0f, 1f)]
		public float TransmissionMix = 0.75f;
		
		[SerializeField][Space(5f)]
		public bool Reflections = true;

		[SerializeField][Range(0f, 1f)]
		public float ReflectionsMix = 0.5f;
	}
}