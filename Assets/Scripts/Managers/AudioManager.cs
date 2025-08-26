using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ScriptableObjects;
using SteamAudio;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace Managers
{
	public class AudioManager : MonoBehaviour
	{
		private static AudioManager instance;
		public static AudioManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				var go = new GameObject("AudioManager");
				DontDestroyOnLoad(go);

				instance = go.AddComponent<AudioManager>();
				instance.loadMixerGroups();
				
				return instance;
			}
		}

		public AudioMixerGroup MasterGroup { get; private set; }
		public AudioMixerGroup SFXGroup { get; private set; }
		
		// source -> target
		private readonly Dictionary<Transform, Transform> attachedSources = new ();
		
		private readonly List<Transform> clearSources = new ();

		#region MonoBehaviour

		public void Update()
		{
			clearSources.Clear();
			
			foreach (var (source, target) in attachedSources)
			{
				if (target == null)
				{
					clearSources.Add(source);
					continue;
				}
				
				source.position = target.position;
			}

			for (var i = clearSources.Count - 1; i >= 0; i--)
				RemoveSource(clearSources[i]);
		}

		#endregion
		
		#region API
		
		public Transform PlayAttached(AudioData audioData, Transform target)
		{
			var source = PlayAtPoint(audioData, target.position);
			attachedSources[source] = target;
			return source;
		}
		
		public Transform PlayAtPoint(AudioData audioData, Vector3 position)
		{
			var go = new GameObject("Audio");
			
			var tr = go.transform;
			tr.position = position;
			
			var clip = Addressables.LoadAssetAsync<AudioClip>(audioData.ClipReferences[Random.Range(0, audioData.ClipReferences.Length)]).WaitForCompletion();

			var audioSource = go.AddComponent<AudioSource>();
			audioSource.spatialize = audioData.Spatialize > 0;
			audioSource.spatialBlend = audioData.Spatialize;
			audioSource.outputAudioMixerGroup = SFXGroup;
			audioSource.volume = audioData.Volume;
			audioSource.loop = audioData.Loop;
			audioSource.playOnAwake = false;
			audioSource.maxDistance = audioData.Distance;
			audioSource.rolloffMode = AudioRolloffMode.Custom;
			audioSource.clip = clip;
			
			var steamAudioSource = go.AddComponent<SteamAudioSource>();
			steamAudioSource.maxTransmissionSurfaces = audioData.TransmissionSurfaces;
			steamAudioSource.distanceAttenuation = audioData.DistanceAttenuation;
			steamAudioSource.airAbsorption = audioData.AirAbsorption;
			steamAudioSource.transmission = audioData.Transmission;
			steamAudioSource.directMixLevel = audioData.TransmissionMix;
			steamAudioSource.reflections = audioData.Reflections;
			steamAudioSource.reflectionsMixLevel = audioData.ReflectionsMix;
			steamAudioSource.occlusion = audioData.Occlusion;
			steamAudioSource.occlusionType = OcclusionType.Volumetric;
			steamAudioSource.interpolation = HRTFInterpolation.Bilinear;
			
			audioSource.Play();
			
			if (!audioData.Loop)
				destroyAfterPlay(tr, clip).Forget();
			
			return tr;
		}

		public void RemoveSource(Transform source)
		{
			if (source == null)
				return;
			
			attachedSources.Remove(source);
			Destroy(source.gameObject);
		}
		
		#endregion

		#region Internals

		private void loadMixerGroups()
		{
			MasterGroup = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Master.mixer[Master]").WaitForCompletion();
			SFXGroup = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Master.mixer[SFX]").WaitForCompletion();
		}

		private async UniTaskVoid destroyAfterPlay(Transform source, AudioClip clip)
		{
			await UniTask.WaitForSeconds(clip.length + 2.5f);
			RemoveSource(source);
		}
		
		#endregion
	}
}