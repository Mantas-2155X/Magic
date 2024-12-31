using System.Runtime.CompilerServices;
using AI;
using AI.Interfaces;
using Combat.Wearables.Enums;
using Combat.Wearables.Interfaces;
using Managers;
using Objects;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Rendering;

namespace Combat.Wearables.Base
{
	public class BaseWearable : MonoBehaviour, IWearable
	{
		[field: SerializeField]
		public WearableData WearableData { get; private set; }

		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }
		[field: SerializeField]
		public Collider[] Colliders { get; set; }
		[field: SerializeField]
		public DroppedWearable DroppedWearable { get; set; }

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		public void Awake()
		{
			initializeObject();
			
			DroppedWearable.Wearable = this;
		}

		public void OnDisable()
		{
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
			if (alive == null || Owner != null)
				return;
			
			Owner = alive;

			DroppedWearable.enabled = false;

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
				hideShadow(true);
		}
		
		public virtual void Drop()
		{
			if (Owner == null)
				return;
			
			if (Owner is Player)
				hideShadow(false);

			var dropTr = Owner.Body.Containers[WearableData.WearableType].Drop;

			var movePos = dropTr.position + (Vector3.down * 0.1f) + (dropTr.right * 0.1f);
			var moveAng = dropTr.eulerAngles;
			
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

			DroppedWearable.enabled = true;

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
		
		private void hideShadow(bool state)
		{
			var renderers = Owner.Body.Containers[WearableData.WearableType].Wear.GetComponentsInChildren<Renderer>(true);
			foreach (var rend in renderers)
				rend.shadowCastingMode = state ? ShadowCastingMode.Off : ShadowCastingMode.On;
		}
	}
}