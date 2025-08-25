using Cysharp.Threading.Tasks;
using ScriptableObjects;
using SteamAudio;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Audio;
using Vector3 = UnityEngine.Vector3;

namespace Audio
{
	public class AudioManager
	{
		private static AudioManager instance;
		public static AudioManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new AudioManager();
				instance.loadMixerGroups();
				
				return instance;
			}
		}

		public AudioMixerGroup MasterGroup { get; private set; }
		public AudioMixerGroup SFXGroup { get; private set; }
		
		public GameObject PlayAtPoint(AudioData audioData, Vector3 position)
		{
			var go = new GameObject("Audio");
			go.transform.position = position;
			
			var clipOperation = Addressables.LoadAssetAsync<AudioClip>(audioData.ClipReference);
			var clip = clipOperation.WaitForCompletion();
			
			Addressables.Release(clipOperation);

			var audioSource = go.AddComponent<AudioSource>();
			audioSource.spatialize = audioData.Spatialize > 0;
			audioSource.spatialBlend = audioData.Spatialize;
			audioSource.outputAudioMixerGroup = SFXGroup;
			audioSource.volume = audioData.Volume;
			audioSource.loop = audioData.Loop;
			audioSource.playOnAwake = false;
			audioSource.clip = clip;
			
			var steamAudioSource = go.AddComponent<SteamAudioSource>();
			steamAudioSource.airAbsorption = audioData.AirAbsorption;
			steamAudioSource.transmission = audioData.Transmission;
			steamAudioSource.reflections = audioData.Reflections;
			steamAudioSource.occlusion = audioData.Occlusion;
			
			audioSource.Play();
			
			if (!audioData.Loop)
				destroyAfterPlay(go, clip).Forget();
			
			return go;
		}

		private void loadMixerGroups()
		{
			var masterOp = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Master.mixer[Master]");
			MasterGroup = masterOp.WaitForCompletion();
			Addressables.Release(masterOp);
			
			var sfxOp = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Master.mixer[SFX]");
			SFXGroup = sfxOp.WaitForCompletion();
			Addressables.Release(sfxOp);
		}

		private async UniTaskVoid destroyAfterPlay(GameObject go, AudioClip clip)
		{
			await UniTask.WaitForSeconds(clip.length + 2.5f);
			
			Object.Destroy(go);
		}
	}
}