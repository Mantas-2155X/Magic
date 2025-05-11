using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Combat.Decals.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Combat.Decals.Base
{
	public class BaseDecal : MonoBehaviour, IDecal
	{
		[field: SerializeField]
		public DecalData DecalData { get; private set; }
		
		public bool ShouldSave => true;

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		[field: SerializeField]
		public DecalProjector Projector { get; private set; }
		
		public IIdentifiable Attach { get; private set; }
		
		public float CreatedTime { get; private set; }
		public float NormalizedTime { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad
		
		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var transformState = TransformState.Read(thisTr);
			if (transformState != null)
				dict[typeof(Transform).ToString()] = JObject.FromObject(transformState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState))
				TransformState.Apply(thisTr, transformState.ToObject<TransformState>());
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
	}
}