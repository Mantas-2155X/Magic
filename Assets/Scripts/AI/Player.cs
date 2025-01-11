using AI.Base;
using AI.Enums;
using Combat.Wearables.Enums;
using Objects.Interfaces;
using ScriptableObjects;
using Tools;
using UI.Hotbar;
using UI.Spellbook;
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
		public InputActionReference ScrollAction;

		[SerializeField]
		public InputActionReference UseAction;

		[SerializeField]
		public InputActionReference AttackAction;
		
		[SerializeField]
		public InputActionReference NoclipAction;
		
		[SerializeField]
		public InputActionReference LightAction;

		[SerializeField]
		public InputActionReference HotbarAction1;
		
		[SerializeField]
		public InputActionReference HotbarAction2;
		
		[SerializeField]
		public InputActionReference HotbarAction3;
		
		[SerializeField]
		public InputActionReference HotbarAction4;
		
		[SerializeField]
		public InputActionReference HotbarAction5;
		
		[SerializeField]
		public InputActionReference HotbarAction6;

		[SerializeField]
		public InputActionReference HotbarAction7;

		[SerializeField]
		public InputActionReference SpellbookAction;

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
			
			if (IsCasting)
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
				// No smoothing for noclip
				Body.Rigidbody.linearVelocity *= data.StopSlide;

				// Grab jump/fall as vertical move direction
				var vertical = jumpPressed ? 1f : fallPressed ? -1f : 0f;
				
				var addVector = new Vector3(moveDirection.x, vertical, moveDirection.y) * (SprintAction.action.IsPressed() ? 1f * data.SprintMultiplier : 1f);
				addVector *= 5f;
				
				Body.Rigidbody.AddRelativeForce(addVector, ForceMode.VelocityChange);
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

		public string GetHotbarKey(int index)
		{
			switch (index)
			{
				case 0:
					return HotbarAction1.action.GetBindingDisplayString();
				case 1:
					return HotbarAction2.action.GetBindingDisplayString();
				case 2:
					return HotbarAction3.action.GetBindingDisplayString();
				case 3:
					return HotbarAction4.action.GetBindingDisplayString();
				case 4:
					return HotbarAction5.action.GetBindingDisplayString();
				case 5:
					return HotbarAction6.action.GetBindingDisplayString();
				case 6:
					return HotbarAction7.action.GetBindingDisplayString();
			}

			return "";
		}
		
		public void EnableInput()
		{
			// Prevent double binds
			DisableInput();
			
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

			var scroll = ScrollAction.action;
			scroll.performed += onScroll;
			scroll.Enable();
			
			var hotbar1 = HotbarAction1.action;
			hotbar1.performed += onHotbar1;
			hotbar1.Enable();
			
			var hotbar2 = HotbarAction2.action;
			hotbar2.performed += onHotbar2;
			hotbar2.Enable();
			
			var hotbar3 = HotbarAction3.action;
			hotbar3.performed += onHotbar3;
			hotbar3.Enable();
			
			var hotbar4 = HotbarAction4.action;
			hotbar4.performed += onHotbar4;
			hotbar4.Enable();
			
			var hotbar5 = HotbarAction5.action;
			hotbar5.performed += onHotbar5;
			hotbar5.Enable();
			
			var hotbar6 = HotbarAction6.action;
			hotbar6.performed += onHotbar6;
			hotbar6.Enable();
			
			var hotbar7 = HotbarAction7.action;
			hotbar7.performed += onHotbar7;
			hotbar7.Enable();
			
			var spellbook = SpellbookAction.action;
			spellbook.performed += onSpellbook;
			spellbook.Enable();
		}

		public void DisableInput(bool includePanels = true)
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

			var scroll = ScrollAction.action;
			scroll.performed -= onScroll;
			scroll.Disable();
			
			var hotbar1 = HotbarAction1.action;
			hotbar1.performed -= onHotbar1;
			hotbar1.Disable();
			
			var hotbar2 = HotbarAction2.action;
			hotbar2.performed -= onHotbar2;
			hotbar2.Disable();
			
			var hotbar3 = HotbarAction3.action;
			hotbar3.performed -= onHotbar3;
			hotbar3.Disable();
			
			var hotbar4 = HotbarAction4.action;
			hotbar4.performed -= onHotbar4;
			hotbar4.Disable();
			
			var hotbar5 = HotbarAction5.action;
			hotbar5.performed -= onHotbar5;
			hotbar5.Disable();
			
			var hotbar6 = HotbarAction6.action;
			hotbar6.performed -= onHotbar6;
			hotbar6.Disable();
			
			var hotbar7 = HotbarAction7.action;
			hotbar7.performed -= onHotbar7;
			hotbar7.Disable();

			if (includePanels)
			{
				var spellbook = SpellbookAction.action;
				spellbook.performed -= onSpellbook;
				spellbook.Disable();
			}
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

		private void onScroll(InputAction.CallbackContext ctx)
		{
			if (Spells.Count < 2)
				return;
			
			var currentIndex = GetSpellIndex(Spell != null ? Spell.SpellData : null);
			currentIndex -= (int)ctx.ReadValue<Vector2>().y;

			var maxSpell = Mathf.Min(Hotbar.Instance.Size, Spells.Count);
			
			if (currentIndex < 0)
				currentIndex = maxSpell - 1;

			if (currentIndex >= maxSpell)
				currentIndex = 0;
			
			SelectSpell(Spells[currentIndex].SpellData);
		}
		
		private void onHotbar1(InputAction.CallbackContext ctx) => SelectSpell(0);
		private void onHotbar2(InputAction.CallbackContext ctx) => SelectSpell(1);
		private void onHotbar3(InputAction.CallbackContext ctx) => SelectSpell(2);
		private void onHotbar4(InputAction.CallbackContext ctx) => SelectSpell(3);
		private void onHotbar5(InputAction.CallbackContext ctx) => SelectSpell(4);
		private void onHotbar6(InputAction.CallbackContext ctx) => SelectSpell(5);
		private void onHotbar7(InputAction.CallbackContext ctx) => SelectSpell(6);
		
		private void onSpellbook(InputAction.CallbackContext ctx) => Spellbook.Instance.Toggle();

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
			
			Spellbook.Instance.Display(false);
			EnableInput();
		}
		
		public override void Kill(object source)
		{
			Body.Containers[EWearableType.Weapon].Wear.SetParent(Body.Shoulders[1]);
		
			World.World.Instance.Flashlight.enabled = false;
			
			Spellbook.Instance.Display(false);
			DisableInput();
			
			setRenderMode(ShadowCastingMode.On);
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