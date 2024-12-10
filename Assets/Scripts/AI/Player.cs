using AI.Base;
using Objects.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using Weapons.Interfaces;

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
		public InputActionReference DropAction;

		#endregion

		[SerializeField]
		public float MovementForce = 1f;
		
		[SerializeField]
		public float JumpForce = 135f;

		[SerializeField]
		public float SprintMultiplier = 1.25f;

		[SerializeField]
		public float LookSensitivity = 0.1f;
		
		[SerializeField]
		public float StopSlide = 0.65f;
		
		[SerializeField]
		public float AirMovement = 0.1f;

		[SerializeField]
		public float SpeedClampModifier = 0.91f;
		
		[SerializeField]
		public float UseDistance = 1.5f;

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

			// Where is the Held mode for the input action?
			if (AttackAction.action.IsPressed())
				Weapon?.Attack();
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
			
			if (IsNoclip)
			{
				Body.Rigidbody.AddRelativeForce(new Vector3(moveDirection.x, 0f, moveDirection.y) * (SprintAction.action.IsPressed() ? 1f * SprintMultiplier : 1f), ForceMode.VelocityChange);
				
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
				velocity.x *= StopSlide;
				velocity.z *= StopSlide;
				
				Body.Rigidbody.linearVelocity = velocity;
				return;
			}

			var movement = MovementForce;
			
			// Adjust how much control force is weakened if not grounded
			if (!grounded)
				movement *= AirMovement;
			
			Body.Rigidbody.AddRelativeForce(new Vector3(moveDirection.x, 0f, moveDirection.y) * movement, ForceMode.VelocityChange);
			
			if (!grounded)
				return;
			
			// Limit the rigidbody walking speed
			var maxSpeed = SprintAction.action.IsPressed() ? MaximumSpeed * SprintMultiplier : MaximumSpeed;
			Body.Rigidbody.linearVelocity = Vector3.ClampMagnitude(Body.Rigidbody.linearVelocity, maxSpeed * SpeedClampModifier);
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
			attack.Enable();
			
			var noclip = NoclipAction.action;
			noclip.performed += onNoclip;
			noclip.Enable();
			
			var drop = DropAction.action;
			drop.performed += onDrop;
			drop.Enable();
			
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
			attack.Disable();
						
			var noclip = NoclipAction.action;
			noclip.performed -= onNoclip;
			noclip.Disable();
			
			var drop = DropAction.action;
			drop.performed -= onDrop;
			drop.Disable();

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
			
			if (!IsNoclip && IsGrounded())
				Body.Rigidbody.AddForce(0f, JumpForce, 0f, ForceMode.Impulse);
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
		
		private void onUse(InputAction.CallbackContext ctx)
		{
			if (!Physics.Raycast(Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, UseDistance, ~LayerMaskTools.Mask1))
				return;
			
			var usable = hit.collider.GetComponent<IUsable>();
			usable?.Use(this);
		}

		private void onNoclip(InputAction.CallbackContext ctx)
		{
			SetNoclip(!IsNoclip);
		}
		
		private void onDrop(InputAction.CallbackContext ctx)
		{
			DropWeapon();
		}

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => walking ? Body.Rigidbody.linearVelocity.magnitude : MaximumSpeed;

		public override bool IsWalking => walking;

		public override void TakeWeapon(IWeapon weapon)
		{
			base.TakeWeapon(weapon);
			
			var transforms = Body.WeaponContainer.GetComponentsInChildren<Transform>();
			foreach (var tr in transforms)
				tr.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
		}
		
		public override void DropWeapon()
		{
			var transforms = Body.WeaponContainer.GetComponentsInChildren<Transform>();
			foreach (var tr in transforms)
				tr.gameObject.layer = 0;
			
			base.DropWeapon();
		}
		
		public override void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed)
		{
			base.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, maximumSpeed);
			enableInput();
		}
		
		public override void Kill(object source)
		{
			disableInput();
			base.Kill(source);
		}
		
		#endregion
	}
}