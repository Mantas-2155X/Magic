using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class AudioData : Data
	{
		[Header("Audio Settings")]
		[SerializeField]
		public AssetReference ClipReference;

		[SerializeField]
		public float Volume = 1f;
		
		[SerializeField]
		public float MaximumDistance = 25f;
		
		[SerializeField]
		public float MinimumDistance = 1f;

		[SerializeField]
		public bool Loop;

		[Header("Spatialization")]
		[SerializeField]
		public float Spatialize = 1f;

		[SerializeField]
		public bool DistanceAttenuation = true;

		[SerializeField]
		public bool AirAbsorption = true;
		
		[SerializeField]
		public bool Transmission = true;
		
		[SerializeField]
		public bool Reflections = true;
		
		[SerializeField]
		public bool Occlusion = true;
	}
}