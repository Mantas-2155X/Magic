using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Base;
using AI.Interfaces;
using Combat.Enums;
using Components;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using State.Enums;
using State.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Player = AI.Player;
using Random = UnityEngine.Random;

namespace World
{
	public class World4 : World, ISaveable
	{
		[SerializeField]
		public float AttackEvery = 1f;

		[SerializeField]
		public float AttackEveryCap = 0.15f;

		[SerializeField]
		public float DamageEvery = 0.25f;

		[SerializeField]
		public float DamageAmount = 3f;

		[SerializeField]
		public float MinimumVelocity = 2f;
		
		[SerializeField]
		public float DivideBy = 1.00425f;

		[SerializeField]
		public Vector2 RangeX = new (-8.27f, 8.27f);

		[SerializeField]
		public Vector2 RangeZ = new (-8.27f, 8.27f);

		[SerializeField]
		public float Y = -0.75f;

		[SerializeField]
		public TMP_Text Timer;

		[SerializeField]
		public TextWalker TextWalker;
		
		public float StartTime { get; private set; }
		public float AttacksStartTime { get; private set; }
		public bool TimerStopped { get; private set; }
		public bool AttacksStarted { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad

		public virtual bool ShouldSave => true;

		public virtual bool ShouldTransfer => false;
		
		public virtual bool ExternallySpawned { get => false; set { } }
		
		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
		public virtual ELoadType LoadType => ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Normal;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			throw new NotImplementedException();
		}

		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(World4).ToString()] = JObject.FromObject(new World4State(this));
			
			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(World4).ToString(), out var world4State) && world4State != null)
				world4State.ToObject<World4State>().Apply(this);
		}
		
		public void SetState(float startTimeElapsed, bool timerStopped, float attackEvery, bool attacksStarted, float attacksStartTimeElapsed)
		{
			var time = Time.time;
			
			StartTime = time - startTimeElapsed;
			TimerStopped = timerStopped;
			AttackEvery = attackEvery;

			if (attacksStarted)
			{
				AttacksStarted = true;
				AttacksStartTime = time - attacksStartTimeElapsed;
				
				startLoops().Forget();
			}
			
			if (!timerStopped)
				return;

			var player = AIManager.Instance.Player;
			if (player == null || !player.IsAlive)
				return;
			
			player.Kill(this, true);
			updateTime();
		}
		
		public override void Awake()
		{
			base.Awake();
			
			StateManager.Instance.RegisterObject(this);
			initializeObject();

			BaseAlive.OnDeathEvent.AddListener(onDeath);
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			
			StateManager.Instance.UnregisterObject(this);

			BaseAlive.OnDeathEvent.RemoveListener(onDeath);
		}
		
		#endregion
		
		public override void Start()
		{
			base.Start();
			
			StartTime = Time.time;
			
			var spawnPoint = SpawnPoints.GetChild(Random.Range(0, SpawnPoints.childCount));
			
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetData<AliveData>("AI_PLAYER_WORLD4_NAME"));
			TextWalker.Walk(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE4_INFO"), 0f, 2f, 0.1f, 0.0025f);
		}

		public void Update()
		{
			if (PauseManager.IsPaused || TimerStopped)
				return;
			
			updateTime();
		}

		public void OnTextWalkerFinished()
		{
			if (AttacksStarted)
				return;
			
			AttacksStarted = true;
			AttacksStartTime = Time.time;

			startLoops().Forget();
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
		
		private void onDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;

			TimerStopped = true;
			respawnDelayed().Forget();
		}

		private void updateTime()
		{
			var timeSpan = TimeSpan.FromSeconds(Time.time - StartTime);
			Timer.text = $"<mspace=0.6em>{timeSpan.Minutes:00}:{timeSpan.Seconds:00}:{timeSpan.Milliseconds:000}</mspace>";
		}
		
		private async UniTaskVoid startLoops()
		{
			attackLoop().Forget();

			var waitDuration = AttacksStartTime + 5f - Time.time;
			if (waitDuration > 0f)
				await UniTask.WaitForSeconds(waitDuration);
			
			damageLoop().Forget();
		}
		
		private async UniTaskVoid attackLoop()
		{
			var incinerate = ObjectManager.Instance.GetData<AttackData>("ATTACK_INCINERATE_NAME");
			
			while (true)
			{
				await UniTask.WaitForSeconds(AttackEvery);

				if (this == null || !isActiveAndEnabled)
					return;

				var hit = new RaycastHit
				{
					point = new Vector3(Random.Range(RangeX.x, RangeX.y), Y, Random.Range(RangeZ.x, RangeZ.y)),
					normal = Vector3.up
				};

				ObjectManager.Instance.CreateAttack(incinerate, this, hit, null);
				
				if (AttackEvery > AttackEveryCap)
					AttackEvery /= DivideBy;
			}
		}
		
		private async UniTaskVoid damageLoop()
		{
			var player = AIManager.Instance.Player;
			
			while (true)
			{
				await UniTask.WaitForSeconds(DamageEvery);

				if (this == null || !isActiveAndEnabled)
					return;
				
				if (player == null || !player.IsAlive)
					return;
				
				if (player.Body.Rigidbody.linearVelocity.magnitude >= MinimumVelocity)
					continue;
				
				player.Damage(DamageAmount, this, EElement.Unknown);
			}
		}

		private async UniTaskVoid respawnDelayed()
		{
			await UniTask.WaitForSeconds(2.5f);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			await SceneManager.Instance.ReloadSceneAsync(true, true, true, 1f);
		}
		
		[JsonObject]
		public class World4State : IState
		{
			[JsonProperty]
			public float StartTimeElapsed;
		
			[JsonProperty]
			public bool TimerStopped;

			[JsonProperty]
			public float AttackEvery;
		
			[JsonProperty]
			public bool AttacksStarted;
		
			[JsonProperty]
			public float AttacksStartTimeElapsed;

			public World4State() { }
			
			public World4State(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not World4 world4)
					return;
				
				var time = Time.time;
				
				StartTimeElapsed = time - world4.StartTime;
				TimerStopped = world4.TimerStopped;
				AttackEvery = world4.AttackEvery;
				AttacksStarted = world4.AttacksStarted;
				AttacksStartTimeElapsed = world4.AttacksStarted ? time - world4.AttacksStartTime : 0f;
			}
			
			public void Apply(object obj)
			{
				if (obj is not World4 world4)
					return;

				world4.SetState(StartTimeElapsed, TimerStopped, AttackEvery, AttacksStarted, AttacksStartTimeElapsed);
			}
		}
	}
}