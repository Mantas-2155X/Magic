using System;
using System.Collections.Generic;
using AI;
using Combat.Spells.Base;
using Managers;
using Objects.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Combat.Spells
{
	public class PhysicsManipulator : BaseSpell
	{
		public IObject Object { get; private set; }

		public static readonly List<IObject> ManipulatingObjects = new ();
		
		private static readonly float moveSpeed = 15f;
		private static readonly float distanceStep = 0.5f;
		private static readonly float minimumDistance = 1.25f;

		private Transform end;
		public Transform End
		{
			get
			{
				if (end != null)
					return end;

				var asset = Addressables.LoadAssetAsync<GameObject>("Spells/Prefabs/Physics Manipulator End.prefab").WaitForCompletion();
				end = Instantiate(asset, World.World.Instance.Ragdolls).transform;
				return end;
			}
		}

		private LineRenderer lineRenderer;
		private Transform cameraTr;
		
		private InputAction grabAction;
		private Vector3 grabPosition;
		private Vector3 grabAngles;
		private float grabDistance;
		private bool grabbing;

		private CollisionDetectionMode grabCollisionDetectionMode;

		public override void Awake()
		{
			base.Awake();
			
			lineRenderer = gameObject.AddComponent<LineRenderer>();
			lineRenderer.startWidth = 0.25f;
			lineRenderer.endWidth = 0.25f;
			lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
			lineRenderer.material = Addressables.LoadAssetAsync<Material>("Spells/Materials/Physics Manipulator.mat").WaitForCompletion();

			lineRenderer.enabled = false;
		}

		public override void OnDestroy()
		{
			base.OnDestroy();
			
			if (lineRenderer != null)
				Destroy(lineRenderer);
		}
		
		public void OnEnable()
		{
			Player.OnScrollEvent?.AddListener(onScroll);
		}

		public void OnDisable()
		{
			Player.OnScrollEvent?.RemoveListener(onScroll);
		}

		public override void Update()
		{
			base.Update();
			
			if (PauseManager.IsPaused)
				return;
			
			if (Object.IsNull())
			{
				if (grabbing)
					releaseObject();
				
				return;
			}
			
			if (lineRenderer == null)
				return;

			var tr = Object.GetTransform();
			var endPosition = tr.position + (tr.right * (grabPosition.x * tr.localScale.x) + tr.up * (grabPosition.y * tr.localScale.y) + tr.forward * (grabPosition.z * tr.localScale.z));
			
			lineRenderer.SetPosition(0, cameraTr.position - cameraTr.up * 0.25f);
			lineRenderer.SetPosition(1, endPosition);
			
			End.position = endPosition;
		}

		public void LateUpdate()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Object.IsNull())
				return;
			
			if (Owner.IsNull() || !Owner.IsAlive || grabAction == null || !grabAction.IsPressed())
				releaseObject();
		}

		public void FixedUpdate()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Object.IsNull())
				return;
			
			handleObject();
		}

		public override bool CanCast()
		{
			return base.CanCast() && Object.IsNull();
		}

		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;

			if (Owner is not Player)
				return false;

			var rb = LastHit.rigidbody;
			if (rb == null || rb.isKinematic || !rb.TryGetComponent<IObject>(out var obj) || obj.IsNull() || obj == Owner.Grabbing)
				return false;

			grabObject(obj);
			return true;
		}

		public override void Unselect()
		{
			base.Unselect();
			releaseObject();
		}

		private void onScroll(Player player, InputDevice device, float value)
		{
			if (Object.IsNull())
				return;

			grabDistance += device is Mouse ? value * distanceStep : value;
		}
		
		private void grabObject(IObject obj)
		{
			Player.PreventHotbarScrolling = true;
			lineRenderer.enabled = true;
			
			End.gameObject.SetActive(true);

			if (Object.NotNull())
				releaseObject();

			Object = obj;

			var rb = obj.Rigidbody;
			var tr = obj.GetTransform();

			cameraTr = ((Player)Owner).CameraTr;
			
			grabAction = SettingsManager.Instance.GetKeybind("keybinds-gameplay-attack").Item1;
			grabPosition = tr.InverseTransformPoint(LastHit.point);
			grabAngles = tr.eulerAngles - Owner.GetTransform().eulerAngles;
			grabDistance = Vector3.Distance(obj.Rigidbody.position + (tr.right * (grabPosition.x * tr.localScale.x) + tr.up * (grabPosition.y * tr.localScale.y) + tr.forward * (grabPosition.z * tr.localScale.z)), ((Player)Owner).CameraTr.position);
			
			grabCollisionDetectionMode = rb.collisionDetectionMode;

			grabbing = true;

			rb.useGravity = false;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			
			ManipulatingObjects.Add(obj);
		}
		
		private void releaseObject()
		{
			Player.PreventHotbarScrolling = false;
			lineRenderer.enabled = false;

			End.gameObject.SetActive(false);
			ManipulatingObjects.Remove(Object);
			
			grabbing = false;

			if (Object.IsNull())
				return;
			
			var rb = Object.Rigidbody;
			rb.useGravity = true;
			rb.collisionDetectionMode = grabCollisionDetectionMode;

			Object = null;
		}
		
		private void handleObject()
		{
			if (Object.IsNull())
				return;

			if (grabDistance < minimumDistance)
				grabDistance = minimumDistance;
			
			var rb = Object.Rigidbody;
			var tr = Object.GetTransform();
			
			var linearVelocity = (cameraTr.position + (cameraTr.forward * grabDistance)) - (rb.position + (tr.right * (grabPosition.x * tr.localScale.x) + tr.up * (grabPosition.y * tr.localScale.y) + tr.forward * (grabPosition.z * tr.localScale.z)));
			rb.linearVelocity = linearVelocity * moveSpeed;

			// todo: make this use velocity
			rb.rotation = Quaternion.Euler(grabAngles + Owner.GetTransform().eulerAngles);
			rb.angularVelocity = Vector3.zero;
		}
	}
}