using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Combat.Decals.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Combat.Decals.Base
{
	public class BaseDecal : MonoBehaviour, IDecal
	{
		[field: SerializeField]
		public DecalData DecalData { get; private set; }

		[field: SerializeField]
		public DecalProjector Projector { get; private set; }
		
		public IIdentifiable Attach { get; private set; }
		
		public float CreatedTime { get; private set; }
		public float NormalizedTime { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad
		
		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => true;
		
		public virtual bool ExternallySpawned { get; set; }

		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
		public virtual ELoadType LoadType => ELoadType.Create;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.VeryLate;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			var createData = new DecalCreateData()
			{
				Name = DecalData.Name,
				AttachObjectID = Attach.NotNull() ? Attach.ObjectID : null,
				NormalizedTime = NormalizedTime,
				ElapsedTime = Time.time - CreatedTime,
				States = GetModifications()
			};

			return JObject.FromObject(createData);
		}
		
		public static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<DecalCreateData>();
			
			var obj = (BaseDecal)ObjectManager.Instance.CreateDecal(ObjectManager.Instance.GetData<DecalData>(createData.Name), Vector3.zero, Quaternion.identity, StateManager.Instance.GetRegisteredObject(createData.AttachObjectID), createData.ElapsedTime, createData.NormalizedTime);
			obj.ObjectID = data.Item1;
			
			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[BaseDecal] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(Transform).ToString()] = JObject.FromObject(new TransformState(thisTr));

			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState) && transformState != null)
				transformState.ToObject<TransformState>().Apply(thisTr);
		}
		
		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		public void Spawn(Vector3 position, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Decals);
				init = true;
			}

			Projector.size = new Vector3(DecalData.Size, DecalData.Size, DecalData.Size / 2f);
			Projector.fadeFactor = 1f;
			
			thisTr.position = position;
			thisTr.rotation = angles;
			
			Attach = attach;
			CreatedTime = Time.time;

			if (attach.NotNull())
				thisTr.parent = attach.GetTransform();
			
			thisTr.Rotate(new Vector3(0, 0, Random.Range(0, 360)), Space.Self);
			thisGo.SetActive(true);
			
			fade(elapsedTime, normalizedTime).Forget();
		}
		
		private async UniTask fade(float elapsedTime = 0f, float normalizedTime = 0f)
		{
			if (elapsedTime < DecalData.FadeAfter)
				await UniTask.WaitForSeconds(DecalData.FadeAfter - elapsedTime);
			
			if (this == null || !isActiveAndEnabled)
				return;

			var duration = DecalData.FadeDuration;
			
			NormalizedTime = normalizedTime;
			while (NormalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null || !isActiveAndEnabled)
					return;

				Projector.fadeFactor = 1 - NormalizedTime;
				
				NormalizedTime += Time.deltaTime / duration;
			}
			
			thisTr.SetParent(World.World.Instance.Decals);
			
			ObjectID = "";
			PoolingManager.Instance.Add(DecalData, thisGo);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		public class DecalCreateData : CreateData
		{
			[JsonProperty]
			public string AttachObjectID;
		
			[JsonProperty]
			public float NormalizedTime;

			[JsonProperty]
			public float ElapsedTime;
		}
	}
}