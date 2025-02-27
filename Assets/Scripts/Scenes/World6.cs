using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using Objects;
using Objects.Base;
using TMPro;
using UnityEngine;

namespace Scenes
{
	public class World6 : MonoBehaviour
	{
		[SerializeField]
		public BaseLight[] Indicators1;
		
		[SerializeField]
		public BaseLight[] Indicators2;
		
		[SerializeField]
		public BaseLight[] Indicators3;

		[SerializeField]
		public BaseLight[] Indicators4;

		[SerializeField]
		public BaseButton LightButton;
		
		[SerializeField]
		public List<STorusWave> Waves;

		[SerializeField]
		public float TimeBetweenWaves = 5f;

		[SerializeField]
		public TMP_Text Info;

		public int CurrentWave { get; private set; } = -1;

		private int currentCharacter;
		private int remainingSpawners;

		public void Start()
		{
			textLoop(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE6_INFO")).Forget();
		}
		
		public void OnSpawnerCleared()
		{
			remainingSpawners--;

			if (remainingSpawners > 0)
				return;
		
			nextWave().Forget();
		}
		
		private async UniTaskVoid nextWave()
		{
			if (CurrentWave != -1)
				Waves[CurrentWave].Spawners.SetActive(false);
			
			CurrentWave++;

			if (CurrentWave >= Waves.Count)
			{
				textLoop(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE6_CLEARED")).Forget();
				await UniTask.WaitForSeconds(10f);
				await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
				return;
			}
			
			var wave = Waves[CurrentWave];
			
			for (var i = 0; i < Indicators1.Length; i++)
				Indicators1[i].Toggle(i < wave.Indicators1);
			
			for (var i = 0; i < Indicators2.Length; i++)
				Indicators2[i].Toggle(i < wave.Indicators2);
			
			for (var i = 0; i < Indicators3.Length; i++)
				Indicators3[i].Toggle(i < wave.Indicators3);
			
			for (var i = 0; i < Indicators4.Length; i++)
				Indicators4[i].Toggle(i < wave.Indicators4);
			
			remainingSpawners = wave.Spawners.GetComponentsInChildren<NPCSpawner>().Length;

			await UniTask.WaitForSeconds(TimeBetweenWaves);
			
			if (this == null || !isActiveAndEnabled)
				return;

			if (wave.ToggleLight)
				LightButton.Use(AIManager.Instance.Player);
		
			wave.Spawners.gameObject.SetActive(true);
		}
		
		private async UniTaskVoid textLoop(string text)
		{
			currentCharacter = 0;

			Info.text = "";
			Info.gameObject.SetActive(true);
		
			while (currentCharacter < text.Length)
			{
				await UniTask.WaitForSeconds(0.1f);
				
				if (this == null || !isActiveAndEnabled)
					return;
				
				Info.text = text[..(currentCharacter + 1)];
				currentCharacter++;
			}

			await UniTask.WaitForSeconds(2f);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			while (currentCharacter >= 0)
			{
				await UniTask.WaitForSeconds(0.0025f);
				
				if (this == null || !isActiveAndEnabled)
					return;
				
				Info.text = text[..currentCharacter];
				currentCharacter--;
			}
			
			Info.gameObject.SetActive(false);

			if (CurrentWave == -1)
				nextWave().Forget();
		}
		
		[Serializable]
		public struct STorusWave
		{
			[SerializeField]
			public GameObject Spawners;

			[SerializeField]
			[Range(0, 3)]
			public int Indicators1;
		
			[SerializeField]
			[Range(0, 3)]
			public int Indicators2;
		
			[SerializeField]
			[Range(0, 3)]
			public int Indicators3;
			
			[SerializeField]
			[Range(0, 3)]
			public int Indicators4;

			[SerializeField]
			public bool ToggleLight;
		}
	}
}