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

		[SerializeField]
		public float Volume = 1f;
		
		[SerializeField]
		public float MaximumDistance = 25f;
		
		[SerializeField]
		public float MinimumDistance = 2f;

		[SerializeField]
		public bool Loop;

		[Header("Spatialization")]
		[SerializeField]
		public float Spatialize = 0.9f;

		[SerializeField]
		public bool DistanceAttenuation = true;

		[SerializeField]
		public bool AirAbsorption = true;
		
		[SerializeField]
		public bool Transmission = true;
		
		[SerializeField]
		public bool Reflections;
		
		[SerializeField]
		public bool Occlusion = true;
	}
}