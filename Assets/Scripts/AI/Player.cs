using AI.Base;
using AI.Enums;
using Combat.Wearables.Enums;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using Tools;
using UI;
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
		public InputActionReference ScrollAction;

		#endregion

		// Overriden by settings, caching here to reduce setting lookups
		public static float MouseSensitivity = 1f;
		public static float ControllerSensitivity = 1f;
		public static bool AllowHotbarScrolling = true;
		
		[SerializeField]
		public float UseDistance = 2.5f;

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

		private bool shouldBreak;
		
		#region MonoBehaviour

		public void Awake()
		{
			Camera = Camera.main;
			CameraTr = Camera!.transform;
		}

		public void OnDestroy()
		{
			DisableInput();
		}

		public void Update()
		{
			if (!IsAlive)
				return;

			if (SettingsManager.Instance.GetKeybind("keybinds-gameplay-attack").Item1.IsPressed() && Spell != null)
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

			CameraTr.position = transform.position + Vector3.up * 0.5f;

			if (Paralyzed || lookDirection == Vector2.zero)
				return;
			
			var cameraAngle = CameraTr.eulerAngles;
			cameraAngle.y += lookDirection.x;

			var cameraAngleX = cameraAngle.x - lookDirection.y;
			
			if (cameraAngleX > 180)
				cameraAngleX -= 360;
			
			if (cameraAngleX > 85)
				cameraAngleX = 85;
			
			if (cameraAngleX < -85)
				cameraAngleX = -85;
			
			cameraAngle.x = cameraAngleX;
			
			CameraTr.eulerAngles = cameraAngle;
			Body.Rigidbody.MoveRotation(Quaternion.Euler(new Vector3(0f, cameraAngle.y, 0f)));
		}

		public void FixedUpdate()
		{
			if (!IsAlive)
				return;

			var data = (PlayerData)Data;
			var sprintAction = SettingsManager.Instance.GetKeybind("keybinds-movement-sprint").Item1;
			
			if (MovementType == EMovementType.Noclip)
			{
				// No smoothing for noclip
				Body.Rigidbody.linearVelocity *= data.StopSlide;

				// Grab jump/fall as vertical move direction
				var vertical = jumpPressed ? 1f : fallPressed ? -1f : 0f;
				
				var addVector = new Vector3(moveDirection.x, vertical, moveDirection.y) * (sprintAction.IsPressed() ? 1f * data.SprintMultiplier : 1f);
				addVector *= 5f;
				
				Body.Rigidbody.AddRelativeForce(addVector, ForceMode.VelocityChange);
				return;
			}
			
			if (moveDirection == Vector2.zero)
				return;

			var isSprinting = false;
			var sprintEnergy = data.SprintEnergy * Time.fixedDeltaTime;
			
			if (CurrentEnergy >= sprintEnergy && sprintAction.IsPressed())
			{
				isSprinting = true;
				TakeEnergy(sprintEnergy, this);
			}
			
			var movement = data.MovementForce;
			var grounded = IsGrounded();

			// Prevent movement when fully bound
			if (Paralyzed || SlowAmount >= 1f)
				movement = 0;
			
			// Adjust how much control force is weakened if not grounded
			if (!grounded)
				movement *= data.AirMovement;
			
			shouldBreak = true;
			Body.Rigidbody.AddRelativeForce(new Vector3(moveDirection.x, 0f, moveDirection.y) * movement, ForceMode.Acceleration);
			
			if (!grounded)
				return;

			var maxSpeed = Paralyzed ? 0f : data.Speed - (data.Speed * SlowAmount);
			
			// Limit the rigidbody walking speed
			var clampSpeed = isSprinting ? maxSpeed * data.SprintMultiplier : maxSpeed;
			Body.Rigidbody.linearVelocity = Vector3.ClampMagnitude(Body.Rigidbody.linearVelocity, clampSpeed * data.SpeedClampModifier);
		}
		
		#endregion

		#region Input

		public string GetHotbarKey(int index)
		{
			switch (index)
			{
				case 0:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar1").Item1.GetBindingDisplayString();
				case 1:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar2").Item1.GetBindingDisplayString();
				case 2:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar3").Item1.GetBindingDisplayString();
				case 3:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar4").Item1.GetBindingDisplayString();
				case 4:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar5").Item1.GetBindingDisplayString();
				case 5:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar6").Item1.GetBindingDisplayString();
				case 6:
					return SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar7").Item1.GetBindingDisplayString();
			}

			return "";
		}
		
		public void EnableInput()
		{
			if (Title.Instance != null && Title.Instance.isActiveAndEnabled)
				return;
			
			// Prevent double binds
			DisableInput();
			
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;

			var look = LookAction.action;
			look.performed += onLookPerformed;
			look.canceled += onLookCanceled;
			look.Enable();
			
			var move = SettingsManager.Instance.GetKeybind("keybinds-movement-forward").Item1;
			move.performed += onMovePerformed;
			move.canceled += onMoveCanceled;
			move.Enable();
			
			var jump = SettingsManager.Instance.GetKeybind("keybinds-movement-jump").Item1;
			jump.performed += onJumpPerformed;
			jump.canceled += onJumpCanceled;
			jump.Enable();
			
			var fall = SettingsManager.Instance.GetKeybind("keybinds-movement-fall").Item1;
			fall.performed += onFallPerformed;
			fall.canceled += onFallCanceled;
			fall.Enable();
			
			var use = SettingsManager.Instance.GetKeybind("keybinds-gameplay-interact").Item1;
			use.performed += onUse;
			use.Enable();
			
			var attack = SettingsManager.Instance.GetKeybind("keybinds-gameplay-attack").Item1;
			attack.performed += onAttackPerformed;
			attack.canceled += onAttackCanceled;
			attack.Enable();
			
			var noclip = SettingsManager.Instance.GetKeybind("keybinds-debug-noclip").Item1;
			noclip.performed += onNoclip;
			noclip.Enable();
			
			var lightA = SettingsManager.Instance.GetKeybind("keybinds-gameplay-light").Item1;
			lightA.performed += onLight;
			lightA.Enable();
			
			var sprint = SettingsManager.Instance.GetKeybind("keybinds-movement-sprint").Item1;
			sprint.Enable();

			var scroll = ScrollAction.action;
			scroll.performed += onScroll;
			scroll.Enable();
			
			var hotbar1 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar1").Item1;
			hotbar1.performed += onHotbar1;
			hotbar1.Enable();
			
			var hotbar2 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar2").Item1;
			hotbar2.performed += onHotbar2;
			hotbar2.Enable();
			
			var hotbar3 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar3").Item1;
			hotbar3.performed += onHotbar3;
			hotbar3.Enable();
			
			var hotbar4 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar4").Item1;
			hotbar4.performed += onHotbar4;
			hotbar4.Enable();
			
			var hotbar5 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar5").Item1;
			hotbar5.performed += onHotbar5;
			hotbar5.Enable();
			
			var hotbar6 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar6").Item1;
			hotbar6.performed += onHotbar6;
			hotbar6.Enable();
			
			var hotbar7 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar7").Item1;
			hotbar7.performed += onHotbar7;
			hotbar7.Enable();
			
			var spellbook = SettingsManager.Instance.GetKeybind("keybinds-gameplay-spellbook").Item1;
			spellbook.performed += onSpellbook;
			spellbook.Enable();
		}

		public void DisableInput(bool includePanels = true)
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			var look = LookAction.action;
			look.Disable();
			look.performed -= onLookPerformed;
			look.canceled -= onLookCanceled;

			var move = SettingsManager.Instance.GetKeybind("keybinds-movement-forward").Item1;
			move.Disable();
			move.performed -= onMovePerformed;
			move.canceled -= onMoveCanceled;

			var jump = SettingsManager.Instance.GetKeybind("keybinds-movement-jump").Item1;
			jump.Disable();
			jump.performed -= onJumpPerformed;
			jump.canceled -= onJumpCanceled;

			var fall = SettingsManager.Instance.GetKeybind("keybinds-movement-fall").Item1;
			fall.Disable();
			fall.performed -= onFallPerformed;
			fall.canceled -= onFallCanceled;

			var use = SettingsManager.Instance.GetKeybind("keybinds-gameplay-interact").Item1;
			use.Disable();
			use.performed -= onUse;

			var attack = SettingsManager.Instance.GetKeybind("keybinds-gameplay-attack").Item1;
			attack.Disable();
			attack.performed -= onAttackPerformed;
			attack.canceled -= onAttackCanceled;
	
			var noclip = SettingsManager.Instance.GetKeybind("keybinds-debug-noclip").Item1;
			noclip.Disable();
			noclip.performed -= onNoclip;

			var lightA = SettingsManager.Instance.GetKeybind("keybinds-gameplay-light").Item1;
			lightA.Disable();
			lightA.performed -= onLight;

			var sprint = SettingsManager.Instance.GetKeybind("keybinds-movement-sprint").Item1;
			sprint.Disable();

			var scroll = ScrollAction.action;
			scroll.Disable();
			scroll.performed -= onScroll;

			var hotbar1 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar1").Item1;
			hotbar1.Disable();
			hotbar1.performed -= onHotbar1;

			var hotbar2 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar2").Item1;
			hotbar2.Disable();
			hotbar2.performed -= onHotbar2;

			var hotbar3 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar3").Item1;
			hotbar3.Disable();
			hotbar3.performed -= onHotbar3;

			var hotbar4 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar4").Item1;
			hotbar4.Disable();
			hotbar4.performed -= onHotbar4;

			var hotbar5 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar5").Item1;
			hotbar5.Disable();
			hotbar5.performed -= onHotbar5;

			var hotbar6 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar6").Item1;
			hotbar6.Disable();
			hotbar6.performed -= onHotbar6;

			var hotbar7 = SettingsManager.Instance.GetKeybind("keybinds-gameplay-hotbar7").Item1;
			hotbar7.Disable();
			hotbar7.performed -= onHotbar7;

			if (includePanels)
			{
				var spellbook = SettingsManager.Instance.GetKeybind("keybinds-gameplay-spellbook").Item1;
				spellbook.Disable();
				spellbook.performed -= onSpellbook;
			}
		}

		private void onLookPerformed(InputAction.CallbackContext ctx)
		{
			lookDirection = ctx.ReadValue<Vector2>();

			switch (ctx.control.device)
			{
				case Mouse or Pointer:
					lookDirection = lookDirection * 0.075f * MouseSensitivity;
					break;
				case Gamepad or Joystick:
					// Why is this framerate dependant but mouse isn't?
					lookDirection = lookDirection * ControllerSensitivity * Time.unscaledDeltaTime * 135f;
					break;
			}
		}
		
		private void onLookCanceled(InputAction.CallbackContext ctx)
		{
			lookDirection = Vector2.zero;
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

			if (!shouldBreak || !IsGrounded())
				return;

			var velocity = Body.Rigidbody.linearVelocity;
			velocity *= 40f;
			
			Body.Rigidbody.AddForce(-velocity, ForceMode.Acceleration);
			shouldBreak = false;
		}
		
		private void onJumpPerformed(InputAction.CallbackContext ctx)
		{
			jumpPressed = true;
			
			if (MovementType == EMovementType.Normal && !Paralyzed && SlowAmount < 1f && IsGrounded())
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

			// Scrollwheel switching might be disabled
			if (!AllowHotbarScrolling && ctx.control.device is Mouse)
				return;
			
			var currentIndex = GetSpellIndex(Spell != null ? Spell.SpellData : null);
			currentIndex -= (int)ctx.ReadValue<Vector2>().y;

			var maxSpell = Mathf.Min(UI.Player.Instance.HUD.Hotbar.Size, Spells.Count);
			
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
		
		private void onSpellbook(InputAction.CallbackContext ctx) => UI.Player.Instance.HUD.Spellbook.Toggle();

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => walking ? Body.Rigidbody.linearVelocity.magnitude : (Paralyzed || SlowAmount >= 1f ? 0f : Data.Speed);

		public override bool IsWalking => walking;

		public override void SetInvulnerable(bool value)
		{
			var previous = IsInvulnerable;
			
			base.SetInvulnerable(value);

			if (previous == value)
				return;

			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, value ? "Enabled god mode" : "Disabled god mode");
		}
		
		public override void SetPowerful(bool value)
		{
			var previous = IsPowerful;
			
			base.SetPowerful(value);

			if (previous == value)
				return;

			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, value ? "Enabled power mode" : "Disabled power mode");
		}
		
		public override void SetMovementType(EMovementType value)
		{
			var previous = MovementType;
			
			base.SetMovementType(value);

			if (previous == value)
				return;

			switch (value)
			{
				case EMovementType.Noclip:
					ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, "Enabled noclip mode");
					break;
				case EMovementType.Normal when previous == EMovementType.Noclip:
					ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, "Disabled noclip mode");
					break;
			}
		}
		
		public override void SetSpellIndex(SpellData data, int index)
		{
			base.SetSpellIndex(data, index);
			
			var playerUI = UI.Player.Instance;
			if (playerUI == null)
				return;
			
			if (Spell != null)
            {
            	// Changing spell index might put it outside of hotbar size, put it to the first one if so
            	if (GetSpellIndex(Spell.SpellData) >= playerUI.HUD.Hotbar.Size)
            		SelectSpell(0);
            }
			
            playerUI.HUD.Hotbar.UpdateHotbar();
			playerUI.HUD.Spellbook.UpdateSpellbook();
		}

		public override void LearnSpell(SpellData data, bool autoSelect)
		{
			base.LearnSpell(data, autoSelect);
			
			var playerUI = UI.Player.Instance;
			if (playerUI == null)
				return;
			
			playerUI.HUD.Hotbar.UpdateHotbar();
			playerUI.HUD.Spellbook.UpdateSpellbook();
		}

		public override void ForgetSpell(SpellData data)
		{
			base.ForgetSpell(data);
			
			var playerUI = UI.Player.Instance;
			if (playerUI == null)
				return;
			
			playerUI.HUD.Hotbar.UpdateHotbar();
			playerUI.HUD.Spellbook.UpdateSpellbook();
		}

		public override void ForgetAllSpells()
		{
			base.ForgetAllSpells();
			
			var playerUI = UI.Player.Instance;
			if (playerUI == null)
				return;
			
			playerUI.HUD.Hotbar.UpdateHotbar();
			playerUI.HUD.Spellbook.UpdateSpellbook();
		}
		
		public override void Spawn(AliveData data, int relationshipGroup)
		{
			var weaponContainer = Body.Containers[EWearableType.Weapon].Wear;
			weaponContainer.SetParent(CameraTr);
			
			weaponContainer.localPosition = ViewmodelPosition;
			weaponContainer.localEulerAngles = ViewmodelAngles;

			SetRenderMode(ShadowCastingMode.ShadowsOnly);
			base.Spawn(data, relationshipGroup);
			
			var playerUI = UI.Player.Instance;
			if (playerUI != null)
				playerUI.HUD.Spellbook.Display(false);
			
			EnableInput();
		}
		
		public override void Kill(object source)
		{
			Body.Containers[EWearableType.Weapon].Wear.SetParent(Body.Shoulders[1]);
		
			World.World.Instance.Flashlight.enabled = false;
			
			var playerUI = UI.Player.Instance;
			if (playerUI != null)
				playerUI.HUD.Spellbook.Display(false);
			
			DisableInput();
			
			SetRenderMode(ShadowCastingMode.On);
			base.Kill(source);
		}

		public override bool IsGrounded()
		{
			if (!IsAlive || MovementType != EMovementType.Normal)
				return false;

			var origin = Body.Rigidbody.position + new Vector3(0f, -1.02f, 0f);
			var extents = new Vector3(0.6f, 0.05f, 0.2f) / 2f;
			
			if (Physics.CheckBox(origin, extents, transform.rotation, ~LayerMaskTools.GetMaskWithPlayer(), QueryTriggerInteraction.Ignore))
				return true;
			
			return false;
		}
		
		#endregion
		
		public void SetRenderMode(ShadowCastingMode mode)
		{
			var renderers = Body.GetComponentsInChildren<Renderer>(true);
			foreach (var rend in renderers)
				rend.shadowCastingMode = mode;
		}
	}
}