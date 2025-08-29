using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers.Enums;
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

				instance.tempListener = go.AddComponent<AudioListener>();
				
				instance.uiSource = go.AddComponent<AudioSource>();
				instance.uiSource.outputAudioMixerGroup = instance.UIGroup;
				instance.uiSource.ignoreListenerPause = true;
				instance.uiSource.ignoreListenerVolume = true;
				instance.uiSource.playOnAwake = false;

				return instance;
			}
		}

		public AudioMixerGroup MasterGroup { get; private set; }
		public AudioMixerGroup SFXGroup { get; private set; }
		public AudioMixerGroup UIGroup { get; private set; }
		
		private readonly Dictionary<Transform, Tuple<Transform, Vector3>> attachedSources = new ();
		private readonly Dictionary<EUIAudio, Tuple<AudioClip, float>> uiClips = new ();
		private readonly Dictionary<SteamAudioMaterial, AudioData> materialDatas = new ();
		
		private readonly List<Transform> clearSources = new ();

		private AudioSource uiSource;
		private AudioListener tempListener;
		
		#region MonoBehaviour

		public void Start()
		{
			Destroy(tempListener);
		}

		public void Update()
		{
			clearSources.Clear();
			
			foreach (var (source, tuple) in attachedSources)
			{
				if (tuple == null || tuple.Item1 == null)
				{
					clearSources.Add(source);
					continue;
				}
				
				var tr = tuple.Item1;
				source.position = tr.position + (tr.right * tuple.Item2.x + tr.up * tuple.Item2.y + tr.forward * tuple.Item2.z);
			}

			for (var i = clearSources.Count - 1; i >= 0; i--)
				RemoveSource(clearSources[i]);
		}

		#endregion
		
		#region API
		
		public Transform PlayAttached(AudioData audioData, Transform target, Vector3 offset)
		{
			var source = PlayAtPoint(audioData, target.position + (target.right * offset.x + target.up * offset.y + target.forward * offset.z));
			attachedSources[source] = new Tuple<Transform, Vector3>(target, offset);
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
			steamAudioSource.directMixLevel = audioData.DirectMix;
			steamAudioSource.reflections = audioData.Reflections;
			steamAudioSource.reflectionsMixLevel = audioData.ReflectionsMix;
			steamAudioSource.occlusion = audioData.Occlusion;
			steamAudioSource.occlusionType = audioData.OcclusionType;
			steamAudioSource.perspectiveCorrection = audioData.PerspectiveCorrection;
			
			audioSource.Play();
			
			if (!audioData.Loop)
				destroyAfterPlay(tr, clip).Forget();
			
			return tr;
		}

		public Transform PlayImpact(SteamAudioMaterial material, Vector3 position)
		{
			if (!materialDatas.TryGetValue(material, out var audioData))
			{
				audioData = Addressables.LoadAssetAsync<AudioData>($"Audio/Hit/{material.name} Hit.asset").WaitForCompletion();
				materialDatas[material] = audioData;
			}

			return PlayAtPoint(audioData, position);
		}
		
		public void PlayUI(EUIAudio audioType)
		{
			if (audioType == EUIAudio.None)
				return;
			
			if (!uiClips.TryGetValue(audioType, out var tuple))
			{
				var audioData = Addressables.LoadAssetAsync<AudioData>($"Audio/UI/UI {audioType}.asset").WaitForCompletion();
				tuple = new Tuple<AudioClip, float>(Addressables.LoadAssetAsync<AudioClip>(audioData.ClipReferences[Random.Range(0, audioData.ClipReferences.Length)]).WaitForCompletion(), audioData.Volume);
				
				uiClips[audioType] = tuple;
			}
			
			uiSource.clip = tuple.Item1;
			uiSource.volume = tuple.Item2;
			
			uiSource.Play();
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
			UIGroup = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Master.mixer[UI]").WaitForCompletion();
		}

		private async UniTaskVoid destroyAfterPlay(Transform source, AudioClip clip)
		{
			await UniTask.WaitForSeconds(clip.length + 2.5f);
			RemoveSource(source);
		}
		
		#endregion
	}
}