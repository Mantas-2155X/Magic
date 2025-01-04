using AI.Base;
using AI.Enums;
using Combat.Wearables.Enums;
using Combat.Wearables.Interfaces;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace AI
{
	public class Player : BaseAlive
	{
		#region Input

		[SerializeField]
		public InputActionReference LookAction;
		
		[SerializeField]
		public InputActionReference MoveAction;

		[SerializeField]
		public InputActionReference JumpAction;
		
		[SerializeField]
		public InputActionReference FallAction;
		
		[SerializeField]
		public InputActionReference SprintAction;

		[SerializeField]
		public InputActionReference UseAction;

		[SerializeField]
		public InputActionReference AttackAction;
		
		[SerializeField]
		public InputActionReference NoclipAction;
		
		[SerializeField]
		public InputActionReference LightAction;

		#endregion

		[SerializeField]
		public float LookSensitivity = 0.1f;
		
		[SerializeField]
		public float UseDistance = 2f;

		[SerializeField]
		public Vector3 ViewmodelPosition = new (0.76f, -1.24f, 1.09f);
		
		[SerializeField]
		public Vector3 ViewmodelAngles = new (15.4f, -20.1f, 0f);
		
		[SerializeField]
		public Vector3 CastViewmodelPosition = new (0.76f, -1.24f, 1.09f);
		
		[SerializeField]
		public Vector3 CastViewmodelAngles = new (50f, -20.1f, 0f);
		
		public Camera Camera { get; private set; }
		public Transform CameraTr { get; private set; }

		private bool walking;
		private bool jumpPressed;
		private bool fallPressed;
		
		private Vector2 lookDirection;
		private Vector2 moveDirection;
		
		#region MonoBehaviour

		public void Awake()
		{
			Camera = Camera.main;
			CameraTr = Camera!.transform;
			lookDirection = new Vector2(transform.eulerAngles.x, transform.eulerAngles.y);
		}

		public void Update()
		{
			if (!IsAlive)
				return;

			if (AttackAction.action.IsPressed() && Spell != null)
				Spell.StartCasting();

			var weaponContainer = Body.Containers[EWearableType.Weapon].Wear;
			
			if (Spell != null && Spell.IsCasting)
			{
				weaponContainer.localPosition = CastViewmodelPosition;
				weaponContainer.localEulerAngles = CastViewmodelAngles;
			}
			else
			{
				weaponContainer.localPosition = ViewmodelPosition;
				weaponContainer.localEulerAngles = ViewmodelAngles;
			}
		}
		
		public void LateUpdate()
		{
			if (!IsAlive)
				return;

			Body.Rigidbody.MoveRotation(Quaternion.Euler(new Vector3(0f, lookDirection.y, 0f)));

			CameraTr.eulerAngles = new Vector3(lookDirection.x, lookDirection.y, 0f);
			CameraTr.position = transform.position + Vector3.up * 0.5f;
		}

		public void FixedUpdate()
		{
			if (!IsAlive)
				return;

			var data = (PlayerData)Data;
			
			if (MovementType == EMovementType.Noclip)
			{
				Body.Rigidbody.AddRelativeForce(new Vector3(moveDirection.x, 0f, moveDirection.y) * (SprintAction.action.IsPressed() ? 1f * data.SprintMultiplier : 1f), ForceMode.VelocityChange);
				
				if (jumpPressed)
					Body.Rigidbody.AddForce(0f, 1f, 0f, ForceMode.VelocityChange);
				else if (fallPressed)
					Body.Rigidbody.AddForce(0f, -1f, 0f, ForceMode.VelocityChange);
				
				return;
			}
			
			var grounded = IsGrounded();

			if (moveDirection == Vector2.zero)
			{
				if (!grounded)
					return;

				// Adjust how fast the rigidbody stops after letting go of controls
				var velocity = Body.Rigidbody.linearVelocity;
				velocity.x *= data.StopSlide;
				velocity.z *= data.StopSlide;
				
				Body.Rigidbody.linearVelocity = velocity;
				return;
			}

			var movement = data.MovementForce;

			// Prevent movement when bound
			if (IsBound)
				movement = 0;
			
			// Adjust how much control force is weakened if not grounded
			if (!grounded)
				movement *= data.AirMovement;
			
			Body.Rigidbody.AddRelativeForce(new Vector3(moveDirection.x, 0f, moveDirection.y) * movement, ForceMode.VelocityChange);
			
			if (!grounded)
				return;

			var maxSpeed = IsBound ? 0f : Data.Speed;
			
			// Limit the rigidbody walking speed
			var clampSpeed = SprintAction.action.IsPressed() ? maxSpeed * data.SprintMultiplier : maxSpeed;
			Body.Rigidbody.linearVelocity = Vector3.ClampMagnitude(Body.Rigidbody.linearVelocity, clampSpeed * data.SpeedClampModifier);
		}
		
		#endregion

		#region Input

		private void enableInput()
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			var look = LookAction.action;
			look.performed += onLookPerformed;
			look.Enable();
			
			var move = MoveAction.action;
			move.performed += onMovePerformed;
			move.canceled += onMoveCanceled;
			move.Enable();
			
			var jump = JumpAction.action;
			jump.performed += onJumpPerformed;
			jump.canceled += onJumpCanceled;
			jump.Enable();
			
			var fall = FallAction.action;
			fall.performed += onFallPerformed;
			fall.canceled += onFallCanceled;
			fall.Enable();
			
			var use = UseAction.action;
			use.performed += onUse;
			use.Enable();
			
			var attack = AttackAction.action;
			attack.performed += onAttackPerformed;
			attack.canceled += onAttackCanceled;
			attack.Enable();
			
			var noclip = NoclipAction.action;
			noclip.performed += onNoclip;
			noclip.Enable();
			
			var lightA = LightAction.action;
			lightA.performed += onLight;
			lightA.Enable();
			
			var sprint = SprintAction.action;
			sprint.Enable();
		}

		private void disableInput()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			var look = LookAction.action;
			look.performed -= onLookPerformed;
			look.Disable();
			
			var move = MoveAction.action;
			move.performed -= onMovePerformed;
			move.canceled -= onMoveCanceled;
			move.Disable();
			
			var jump = JumpAction.action;
			jump.performed -= onJumpPerformed;
			jump.canceled -= onJumpCanceled;
			jump.Disable();
			
			var fall = FallAction.action;
			fall.performed -= onFallPerformed;
			fall.canceled -= onFallCanceled;
			fall.Disable();
			
			var use = UseAction.action;
			use.performed -= onUse;
			use.Disable();
			
			var attack = AttackAction.action;
			attack.performed -= onAttackPerformed;
			attack.canceled -= onAttackCanceled;
			attack.Disable();
						
			var noclip = NoclipAction.action;
			noclip.performed -= onNoclip;
			noclip.Disable();
			
			var lightA = LightAction.action;
			lightA.performed -= onLight;
			lightA.Disable();

			var sprint = SprintAction.action;
			sprint.Disable();
		}

		private void onLookPerformed(InputAction.CallbackContext ctx)
		{
			var value = ctx.ReadValue<Vector2>();
			lookDirection += new Vector2(-value.y, value.x) * LookSensitivity;

			if (lookDirection.x > 85)
				lookDirection.x = 85;
			
			if (lookDirection.x < -85)
				lookDirection.x = -85;
		}

		private void onMovePerformed(InputAction.CallbackContext ctx)
		{
			moveDirection = ctx.ReadValue<Vector2>();
			walking = true;
			Body.ShouldSway = true;
		}
		
		private void onMoveCanceled(InputAction.CallbackContext ctx)
		{
			moveDirection = Vector2.zero;
			walking = false;
		}
		
		private void onJumpPerformed(InputAction.CallbackContext ctx)
		{
			jumpPressed = true;
			
			if (MovementType == EMovementType.Normal && !IsBound && IsGrounded())
				Body.Rigidbody.AddForce(0f, ((PlayerData)Data).JumpForce, 0f, ForceMode.Impulse);
		}
		
		private void onJumpCanceled(InputAction.CallbackContext ctx)
		{
			jumpPressed = false;
		}
		
		private void onFallPerformed(InputAction.CallbackContext ctx)
		{
			fallPressed = true;
		}

		private void onFallCanceled(InputAction.CallbackContext ctx)
		{
			fallPressed = false;
		}
		
		private void onAttackPerformed(InputAction.CallbackContext ctx)
		{
			if (Spell != null)
				Spell.StartCasting();
		}

		private void onAttackCanceled(InputAction.CallbackContext ctx)
		{
			if (Spell != null)
				Spell.CancelCasting();
		}

		private void onUse(InputAction.CallbackContext ctx)
		{
			if (!Physics.Raycast(Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, UseDistance, ~LayerMaskTools.GetMaskWithPlayerAndWater()))
				return;
			
			if (!hit.collider.TryGetComponent<IObject>(out var obj) || !obj.CanUse(this))
				return;

			obj.Use(this);
		}

		private void onNoclip(InputAction.CallbackContext ctx)
		{
			switch (MovementType)
			{
				case EMovementType.Normal:
					SetMovementType(EMovementType.Noclip);
					break;
				case EMovementType.Noclip:
					SetMovementType(EMovementType.Normal);
					break;
			}
		}
		
		private void onLight(InputAction.CallbackContext ctx)
		{
			World.World.Instance.Flashlight.enabled = !World.World.Instance.Flashlight.enabled;
		}

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => walking ? Body.Rigidbody.linearVelocity.magnitude : (IsBound ? 0f : Data.Speed);

		public override bool IsWalking => walking;

		public override void Spawn(AliveData data, int relationshipGroup)
		{
			var weaponContainer = Body.Containers[EWearableType.Weapon].Wear;
			weaponContainer.SetParent(CameraTr);
			
			weaponContainer.localPosition = ViewmodelPosition;
			weaponContainer.localEulerAngles = ViewmodelAngles;

			setRenderMode(ShadowCastingMode.ShadowsOnly);
			base.Spawn(data, relationshipGroup);
			enableInput();
		}
		
		public override void Kill(object source)
		{
			Body.Containers[EWearableType.Weapon].Wear.SetParent(Body.Shoulders[1]);
		
			World.World.Instance.Flashlight.enabled = false;

			setRenderMode(ShadowCastingMode.On);
			disableInput();
			base.Kill(source);
		}

		public override bool IsGrounded()
		{
			if (MovementType != EMovementType.Normal)
				return false;

			var origin = Body.Rigidbody.position + new Vector3(0f, -1.02f, 0f);
			var extents = new Vector3(0.6f, 0.05f, 0.2f) / 2f;
			
			if (Physics.CheckBox(origin, extents, transform.rotation, ~LayerMaskTools.GetMaskWithPlayer(), QueryTriggerInteraction.Ignore))
				return true;
			
			return false;
		}

		private void setRenderMode(ShadowCastingMode mode)
		{
			var renderers = Body.GetComponentsInChildren<Renderer>(true);
			foreach (var rend in renderers)
				rend.shadowCastingMode = mode;
		}
		
		#endregion
	}
}