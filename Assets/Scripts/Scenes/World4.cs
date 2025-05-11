using System;
using System.Runtime.CompilerServices;
using AI.Base;
using AI.Interfaces;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using State.Interfaces;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using Player = AI.Player;
using Random = UnityEngine.Random;

namespace Scenes
{
	public class World4 : MonoBehaviour, IIdentifiable
	{
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
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
		
		private float startTime;
		private bool stopTimer;

		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad

		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();

			BaseAlive.OnDeathEvent.AddListener(onDeath);
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);

			BaseAlive.OnDeathEvent.RemoveListener(onDeath);
		}
		
		#endregion
		
		public void Start()
		{
			startTime = Time.time;
			
			var world = World.World.Instance;
			var spawnPoint = world.SpawnPoints.GetChild(Random.Range(0, world.SpawnPoints.childCount));
			
			AIManager.Instance.CreatePlayer(spawnPoint.position, spawnPoint.eulerAngles, (PlayerData)ObjectManager.Instance.GetAlive("AI_PLAYER_WORLD4_NAME"));
			
			TextWalker.Walk(LocalizationManager.Instance.GetLocalizedEntry("SCENES_SCENE4_INFO"), 0f, 2f, 0.1f, 0.0025f, delegate
			{
				startLoops().Forget();
			});
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (stopTimer)
				return;
			
			var timeSpan = TimeSpan.FromSeconds(Time.time - startTime);
			Timer.text = $"<mspace=0.6em>{timeSpan.Minutes:00}:{timeSpan.Seconds:00}:{timeSpan.Milliseconds:000}</mspace>";
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

			stopTimer = true;
			respawnDelayed().Forget();
		}

		private async UniTaskVoid startLoops()
		{
			attackLoop().Forget();
			await UniTask.WaitForSeconds(5f);
			damageLoop().Forget();
		}
		
		private async UniTaskVoid attackLoop()
		{
			var incinerate = ObjectManager.Instance.GetAttack("ATTACK_INCINERATE_NAME");
			
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
	}
}