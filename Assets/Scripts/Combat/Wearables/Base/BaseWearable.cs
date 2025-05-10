using System.Runtime.CompilerServices;
using AI;
using AI.Interfaces;
using Combat.Wearables.Enums;
using Combat.Wearables.Interfaces;
using Managers;
using Objects;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Combat.Wearables.Base
{
	public class BaseWearable : MonoBehaviour, IWearable
	{
		[field: SerializeField]
		public WearableData WearableData { get; private set; }

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }
		[field: SerializeField]
		public Collider[] Colliders { get; set; }

		private DroppedWearable droppedWearable;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		private bool ignorePooling;
		
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

		public void OnDisable()
		{
			if (ignorePooling)
				return;
			
			ObjectID = "";
			PoolingManager.Instance.Add(WearableData, gameObject);
		}

		public virtual void Spawn(Vector3 position, Vector3 angles)
		{
			initializeObject();
			
			thisTr.position = position;
			thisTr.eulerAngles = angles;
			
			thisGo.SetActive(true);
		}
		
		public virtual void Equip(IAlive alive)
		{
			if (alive.IsNull() || Owner.NotNull())
				return;
			
			Owner = alive;

			if (droppedWearable != null)
			{
				ObjectID = droppedWearable.ObjectID;
				Destroy(droppedWearable);
			}

			Rigidbody.isKinematic = true;
			Rigidbody.detectCollisions = false;
			Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			Rigidbody.interpolation = RigidbodyInterpolation.None;

			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = false;
			
			thisTr.SetParent(Owner.Body.Containers[WearableData.WearableType].Wear);
			thisTr.localPosition = Vector3.zero;
			thisTr.localEulerAngles = Vector3.zero;

			if (Owner is Player)
				setRenderMode(WearableData.WearableType == EWearableType.Weapon ? ShadowCastingMode.Off : ShadowCastingMode.ShadowsOnly);
		}
		
		public virtual void Drop()
		{
			var movePos = Vector3.zero;
			var moveAng = Vector3.zero;
			
			if (Owner.NotNull())
			{
				if (Owner is Player)
					setRenderMode(ShadowCastingMode.On);
				
				var dropTr = Owner.Body.Containers[WearableData.WearableType].Drop;

				movePos = dropTr.position + (Vector3.down * 0.1f) + (dropTr.right * 0.1f);
				moveAng = dropTr.eulerAngles;
			}
			
			thisTr.SetParent(World.World.Instance.Objects);
			thisTr.position = movePos;
			thisTr.eulerAngles = moveAng;
			
			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = true;
			
			Rigidbody.isKinematic = false;
			Rigidbody.detectCollisions = true;
			Rigidbody.constraints = RigidbodyConstraints.None;
			Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			
			Rigidbody.MovePosition(movePos);
			Rigidbody.MoveRotation(Quaternion.Euler(moveAng));

			if (droppedWearable == null)
			{
				// Needed to prevent OnEnable getting called on DroppedWearable before the data is set
				ignorePooling = true;
				thisGo.SetActive(false);
				
				droppedWearable = thisGo.AddComponent<DroppedWearable>();
				droppedWearable.ObjectData = ObjectManager.Instance.GetObject("OBJECT_DROPPEDWEARABLE_NAME");
				droppedWearable.ObjectID = ObjectID;
				droppedWearable.Rigidbody = Rigidbody;
				droppedWearable.Wearable = this;

				ignorePooling = false;
				thisGo.SetActive(true);
			}

			Owner = null;
		}

		public IAlive GetAlive()
		{
			return Owner;
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
		
		private void setRenderMode(ShadowCastingMode mode)
		{
			var renderers = Owner.Body.Containers[WearableData.WearableType].Wear.GetComponentsInChildren<Renderer>(true);
			foreach (var rend in renderers)
				rend.shadowCastingMode = mode;
		}
	}
}