using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using State.Interfaces;
using State.States;
using UI;
using UnityEngine;
using UnityEngine.Serialization;

namespace Scenes
{
	public class World7 : MonoBehaviour, ISaveable
	{
		public bool ShouldSave => true;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set
			{
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects.Remove(objectID);
				objectID = value;
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects[objectID] = this;
			}
		}

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

		#region Identify / SaveLoad
		
		public Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();
			
			var world7State = World7State.Read(this);
			if (world7State != null)
				dict[typeof(World7).ToString()] = JObject.FromObject(world7State);
			
			return dict;
		}

		public void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(World7).ToString(), out var world7State))
				World7State.Apply(this, world7State.ToObject<World7State>());
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

		public void Awake()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects[ObjectID] = this;
		}

		public void OnDestroy()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects.Remove(ObjectID);
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

		private async UniTaskVoid processOrb()
		{
			await UniTask.WaitForSeconds(startDelay);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
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

			await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
		}
	}
}