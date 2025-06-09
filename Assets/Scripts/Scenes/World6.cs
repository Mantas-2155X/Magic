using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Components;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects;
using Objects.Base;
using State.Interfaces;
using State.States;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scenes
{
	public class World6 : MonoBehaviour, ISaveable
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
		public BaseLight[] Lights;
		
		[SerializeField]
		public List<STorusWave> Waves;

		[SerializeField]
		public TextWalker TextWalker;

		public int CurrentWave { get; private set; }
		public int RemainingSpawners { get; private set; }
		
		public float WaveStartTime { get; private set; }
		
		public bool WorldStarted { get; private set; }
		public bool WorldEnded { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		#region Identify / SaveLoad

		public virtual bool ShouldSave => true;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(World6).ToString()] = JObject.FromObject(new World6State(this));
			
			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(World6).ToString(), out var world6State) && world6State != null)
				world6State.ToObject<World6State>().Apply(this);
		}
		
		public void SetState(int currentWave, int remainingSpawners, float waveStartElapsed, bool worldStarted, bool worldEnded)
		{
			CurrentWave = currentWave;
			RemainingSpawners = remainingSpawners;
			WorldStarted = worldStarted;
			WorldEnded = worldEnded;

			if (worldEnded)
			{
				endWorld().Forget();
				return;
			}
			
			if (CurrentWave >= Waves.Count && RemainingSpawners == 0)
				return;
			
			if (worldStarted)
				startWave(waveStartElapsed).Forget();
		}
		
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
			
			RemainingSpawners = Waves[CurrentWave].Spawners.Length;
			startWave().Forget();
		}
		
		public void OnTextWalkerFinished()
		{
			if (CurrentWave >= Waves.Count)
			{
				if (WorldEnded)
					return;

				WorldEnded = true;
				endWorld().Forget();
			}
			else
			{
				if (WorldStarted || WorldEnded)
					return;
				
				RemainingSpawners = Waves[CurrentWave].Spawners.Length;
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

		private async UniTaskVoid startWave(float elapsedTime = 0f)
		{
			WorldStarted = true;
			WaveStartTime = Time.time - elapsedTime;
			
			var wave = Waves[CurrentWave];
			
			for (var i = 0; i < Indicators1.Length; i++)
				Indicators1[i].Toggle(i < wave.Indicators1);
			
			for (var i = 0; i < Indicators2.Length; i++)
				Indicators2[i].Toggle(i < wave.Indicators2);
			
			for (var i = 0; i < Indicators3.Length; i++)
				Indicators3[i].Toggle(i < wave.Indicators3);
			
			for (var i = 0; i < Indicators4.Length; i++)
				Indicators4[i].Toggle(i < wave.Indicators4);
			
			if (elapsedTime < 5f)
				await UniTask.WaitForSeconds(5f - elapsedTime);
			
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
		
		[JsonObject]
		public class World6State : IState
		{
			[JsonProperty]
			public int CurrentWave;
		
			[JsonProperty]
			public int RemainingSpawners;
		
			[JsonProperty]
			public float WaveStartElapsed;
		
			[JsonProperty]
			public bool WorldStarted;
		
			[JsonProperty]
			public bool WorldEnded;

			public World6State() { }
			
			public World6State(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not World6 world6)
					return;

				CurrentWave = world6.CurrentWave;
				RemainingSpawners = world6.RemainingSpawners;
				WaveStartElapsed = world6.WorldStarted ? Time.time - world6.WaveStartTime : 0f;
				WorldStarted = world6.WorldStarted;
				WorldEnded = world6.WorldEnded;
			}
			
			public void Apply(object obj)
			{
				if (obj is not World6 world6)
					return;

				world6.SetState(CurrentWave, RemainingSpawners, WaveStartElapsed, WorldStarted, WorldEnded);
			}
		}
	}
}