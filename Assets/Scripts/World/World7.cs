using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Interfaces;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace World
{
	public class World7 : World, ISaveable
	{
		[SerializeField]
		public ParticleSystem Orb;

		[SerializeField]
		public ParticleSystem SmallOrbs;

		[SerializeField]
		public Transform OrbTr;

		[SerializeField]
		public Light Light;
		
		public float CurrentSize { get; private set; } = 0.0001f;
		public float CurrentBounceIntensity { get; private set; }
		public float ActivatedTime { get; private set; } = -1f;
		public bool PlayerIncluded { get; private set; }
		
		private readonly float maximumSize = 20f;
		private readonly float lightTriggerSize = 15f;

		private float startDelay = 2.5f;
		private readonly float stepDelay = 0.1f;

		private readonly float stepAmount = 0.15f;
		private readonly float lightBounceStep = 0.35f;
		
		private readonly float radius = 50f;
		
		private readonly List<Rigidbody> objects = new ();
		private readonly Collider[] results = new Collider[500];

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
			dict[typeof(World7).ToString()] = JObject.FromObject(new World7State(this));
			
			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(World7).ToString(), out var world7State) && world7State != null)
				world7State.ToObject<World7State>().Apply(this);
		}

		public void SetState(bool activated, bool playerIncluded, float size, float bounceIntensity, float elapsedTime)
		{
			if (!activated)
				return;

			if (playerIncluded)
				objects.Add(AIManager.Instance.Player.Body.Rigidbody);
			
			CurrentSize = size;
			CurrentBounceIntensity = bounceIntensity;

			var simulateOrb = 0f;
			var simulateSmallOrbs = 0f;
			
			if (elapsedTime >= startDelay)
			{
				simulateOrb = elapsedTime - startDelay;

				if (elapsedTime >= startDelay + 5f)
					simulateSmallOrbs = elapsedTime - startDelay + 5f;
				
				startDelay = 0f;
			}
			else
			{
				startDelay -= elapsedTime;
			}

			BeginOrb();

			if (simulateOrb > 0f)
			{
				Orb.Simulate(simulateOrb, false);
				Orb.Play(false);
				
				SmallOrbs.Simulate(simulateSmallOrbs, false);
				SmallOrbs.Play(false);
			}
		}

		public override void Awake()
		{
			base.Awake();
			
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		public void BeginOrb()
		{
			var player = Player.Instance;
			player.HUD.gameObject.SetActive(false);
			player.Stats.gameObject.SetActive(false);
			player.Notice.gameObject.SetActive(false);
			
			var playerRigidbody = AIManager.Instance.Player.Body.Rigidbody;

			var count = Physics.OverlapSphereNonAlloc(Orb.transform.position, radius, results);
			for (var i = 0; i < count; i++)
			{
				var rb = results[i].attachedRigidbody;
				if (rb == null)
					continue;

				if (playerRigidbody == rb)
					PlayerIncluded = true;
				
				objects.Add(rb);
			}

			ActivatedTime = Time.time;
			processOrb().Forget();
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
		
		private async UniTaskVoid processOrb()
		{
			await UniTask.WaitForSeconds(startDelay);
			
			if (this == null || !isActiveAndEnabled)
				return;

			// Objects will kill the player with the huge velocities. Just godmode as its the end anyway
			if (PlayerIncluded)
			{
				var player = AIManager.Instance.Player;
				if (player != null)
					player.SetInvulnerable(true);
			}
			
			OrbTr.parent.gameObject.SetActive(true);

			var orbPos = OrbTr.position;

			while (CurrentSize < maximumSize)
			{
				await UniTask.WaitForSeconds(stepDelay);
				
				if (this == null || !isActiveAndEnabled)
					return;
				
				CurrentSize += stepAmount;
				
				var shape = Orb.shape;
				shape.radius = CurrentSize;

				for (var i = 0; i < objects.Count; i++)
				{
					var obj = objects[i];
					if (obj == null)
						continue;
					
					obj.AddForce((orbPos - obj.position).normalized * 1000 * CurrentSize);
				}

				if (CurrentSize > lightTriggerSize)
				{
					CurrentBounceIntensity += lightBounceStep;
					Light.bounceIntensity = CurrentBounceIntensity;
				}
			}

			await SceneManager.Instance.ChangeSceneAsync(ObjectManager.Instance.GetScene("SCENE_TITLE_NAME"), true, true, false);
		}
		
		[JsonObject]
		public class World7State : IState
		{
			[JsonProperty]
			public bool OrbActivated;

			[JsonProperty]
			public bool PlayerIncluded;
		
			[JsonProperty]
			public float OrbSize;

			[JsonProperty]
			public float OrbLightBounceIntensity;

			[JsonProperty]
			public float ElapsedTime;

			public World7State() { }
			
			public World7State(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not World7 world7)
					return;

				OrbActivated = world7.ActivatedTime >= 0f;
				PlayerIncluded = world7.PlayerIncluded;
				OrbSize = world7.CurrentSize;
				OrbLightBounceIntensity = world7.CurrentBounceIntensity;
				ElapsedTime = world7.ActivatedTime >= 0f ? Time.time - world7.ActivatedTime : 0f;
			}
			
			public void Apply(object obj)
			{
				if (obj is not World7 world7)
					return;

				world7.SetState(OrbActivated, PlayerIncluded, OrbSize, OrbLightBounceIntensity, ElapsedTime);
			}
		}
	}
}