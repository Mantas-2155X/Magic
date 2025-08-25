using Cysharp.Threading.Tasks;
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
				return instance;
			}
		}

		public void PlayAtPoint(AssetReference clipReference, Vector3 position)
		{
			var go = new GameObject("Audio");
			go.transform.position = position;
			
			var clipOperation = Addressables.LoadAssetAsync<AudioClip>(clipReference);
			var clip = clipOperation.WaitForCompletion();
			
			Addressables.Release(clipOperation);

			var groupOperation = Addressables.LoadAssetAsync<AudioMixerGroup>("Assets/Audio/Master.mixer[SFX]");
			var group = groupOperation.WaitForCompletion();
			
			Addressables.Release(groupOperation);

			var audioSource = go.AddComponent<AudioSource>();
			audioSource.outputAudioMixerGroup = group;
			audioSource.playOnAwake = false;
			audioSource.spatialize = true;
			audioSource.spatialBlend = 1f;
			audioSource.loop = false;
			audioSource.clip = clip;
			
			var steamAudioSource = go.AddComponent<SteamAudioSource>();
			steamAudioSource.airAbsorption = true;
			steamAudioSource.transmission = true;
			steamAudioSource.reflections = true;
			steamAudioSource.occlusion = true;
			
			audioSource.Play();
			
			destroyAfterPlay(go, clip).Forget();
		}

		private async UniTaskVoid destroyAfterPlay(GameObject go, AudioClip clip)
		{
			await UniTask.WaitForSeconds(clip.length + 2.5f);
			
			Object.Destroy(go);
		}
	}
}