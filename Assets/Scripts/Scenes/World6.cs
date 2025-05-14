using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Components;
using Cysharp.Threading.Tasks;
using Managers;
using Objects;
using Objects.Base;
using State.Interfaces;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scenes
{
	public class World6 : MonoBehaviour, IIdentifiable
	{
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		[SerializeField]
		public BaseLight[] Indicators1;
		
		[SerializeField]
		public BaseLight[] Indicators2;
		
		[SerializeField]
		public BaseLight[] Indicators3;

		[SerializeField]
		public BaseLight[] Indicators4;

		[SerializeField]
		public BaseLight[] Lights;
		
		[SerializeField]
		public List<STorusWave> Waves;

		[SerializeField]
		public float TimeBetweenWaves = 5f;

		[SerializeField]
		public TextWalker TextWalker;

		public int CurrentWave { get; private set; }
		public int RemainingSpawners { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		#region Identify / SaveLoad

		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion

		public void Start()
		{
			TextWalker.Walk(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE6_INFO"), 0f, 2f, 0.1f, 0.0025f);
		}
		
		public void OnSpawnerCleared()
		{
			RemainingSpawners--;

			if (RemainingSpawners > 0)
				return;
		
			CurrentWave++;

			if (CurrentWave >= Waves.Count)
			{
				TextWalker.Walk(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE6_CLEARED"), 0f, 2f, 0.1f, 0.0025f);
				return;
			}
			
			startWave().Forget();
		}
		
		public void OnTextWalkerFinished()
		{
			if (CurrentWave >= Waves.Count)
			{
				endWorld().Forget();
			}
			else
			{
				startWave().Forget();
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}

		private async UniTaskVoid startWave()
		{
			var wave = Waves[CurrentWave];
			
			for (var i = 0; i < Indicators1.Length; i++)
				Indicators1[i].Toggle(i < wave.Indicators1);
			
			for (var i = 0; i < Indicators2.Length; i++)
				Indicators2[i].Toggle(i < wave.Indicators2);
			
			for (var i = 0; i < Indicators3.Length; i++)
				Indicators3[i].Toggle(i < wave.Indicators3);
			
			for (var i = 0; i < Indicators4.Length; i++)
				Indicators4[i].Toggle(i < wave.Indicators4);
			
			RemainingSpawners = wave.Spawners.Length;

			await UniTask.WaitForSeconds(TimeBetweenWaves);
			
			if (this == null || !isActiveAndEnabled)
				return;

			if (wave.ToggleLight)
				for (var i = 0; i < Lights.Length; i++)
					Lights[i].Toggle(false);
		
			for (var i = 0; i < wave.Spawners.Length; i++)
				wave.Spawners[i].Trigger();
		}
		
		private async UniTaskVoid endWorld()
		{
			await UniTask.WaitForSeconds(5f);
			await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
		}
		
		[Serializable]
		public struct STorusWave
		{
			[SerializeField]
			public NPCSpawner[] Spawners;

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