using System.Threading;
using AI;
using AI.ActionModes;
using AI.Enums;
using Cysharp.Threading.Tasks;
using Objects.Base;
using Objects.Enums;
using ScriptableObjects;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshElevatorLink : MonoBehaviour
{
	[SerializeField]
	public BaseElevator Elevator;
	
	[SerializeField]
	public BaseButton ElevateButton;
	
	[SerializeField]
	public BaseButton LowerButton;

	[SerializeField]
	public Transform StepTarget;

	[SerializeField]
	public Transform UpperLink;
	
	[SerializeField]
	public Transform LowerLink;
	
	public bool IsPartial { get; private set; }
	
	public NPC ButtonUser { get; private set; }
	
	public NPC PlatformUser { get; private set; }

	public bool IsSteppingOn { get; private set; }
	
	public bool IsSteppingOff { get; private set; }
	
	private CancellationTokenSource cancellationToken = new ();
	
	public void Update()
	{
		if (ButtonUser != null)
		{
			// Dying or getting interrupted should clear the user to prevent a lock
			if (ButtonUser.IsAlive && ButtonUser.ActionMode == EActionMode.Use)
				return;

			ButtonUser = null;
		}

		if (PlatformUser != null)
		{
			// Dying should clear the user to prevent a lock
			if (!PlatformUser.IsAlive)
			{
				PlatformUser = null;
				IsSteppingOn = false;
				IsSteppingOff = false;
				return;
			}

			if (!IsSteppingOn && !IsSteppingOff)
			{
				var tr = PlatformUser.GetTransform();
				var targetPosition = Vector3.zero;
				
				switch (Elevator.State)
				{
					case EElevatorState.Elevated or EElevatorState.Elevating:
					{
						targetPosition = UpperLink.position - tr.position;
						break;
					}
					case EElevatorState.Lowered or EElevatorState.Lowering:
					{
						targetPosition = LowerLink.position - tr.position;
						break;
					}
				}
				
				targetPosition.y = 0;

				var targetRotation = Quaternion.LookRotation(targetPosition);
				var rotationSpeed = ((NPCData)PlatformUser.Data).RotationSpeed;
				
				tr.rotation = Quaternion.RotateTowards(tr.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}
	}
	
	public void OnElevatorElevated()
	{
		ButtonUser = null;
		IsPartial = false;
	}

	public void OnElevatorLowered()
	{
		ButtonUser = null;
		IsPartial = false;
	}
	
	public void OnElevatorElevating()
	{
		IsPartial = true;
	}
	
	public void OnElevatorLowering()
	{
		IsPartial = true;
	}

	public bool TryUse(NPC user, BaseButton button)
	{
		if (user.ActionMode == EActionMode.Use)
			return false;

		ButtonUser = user;
		
		var actionMode = (Use)user.ActionModes[EActionMode.Use];
		actionMode.WalkAfterwards = user.Destination;

		user.Use(button);
		return true;
	}

	public void GetOnPlatform(NPC user, bool elevate)
	{
		if (IsSteppingOn)
			return;
		
		PlatformUser = user;
		PlatformUser.Agent.updateRotation = false;

		IsSteppingOn = true;
		
		cancellationToken?.Cancel();
		cancellationToken = new CancellationTokenSource();

		stepOnPlatform(cancellationToken.Token, elevate).Forget();
	}
	
	public void GetOffPlatform()
	{
		if (IsSteppingOff)
			return;
		
		IsSteppingOff = true;

		cancellationToken?.Cancel();
		cancellationToken = new CancellationTokenSource();
		
		stepOffPlatform(cancellationToken.Token).Forget();
	}

	private async UniTaskVoid stepOnPlatform(CancellationToken token, bool elevate)
	{
		var tr = PlatformUser.GetTransform();
		var startPos = tr.position;
		var endPos = StepTarget.position + Vector3.up * (PlatformUser.Agent.baseOffset * tr.localScale.y);
		var speed = PlatformUser.Data.Speed;
		
		var normalizedTime = 0.0f;
		while (normalizedTime < 1.0f)
		{
			await UniTask.NextFrame();
			
			if (this == null)
				return;
			
			if (PlatformUser == null || !PlatformUser.IsAlive)
			{
				PlatformUser = null;
				IsSteppingOn = false;
				IsSteppingOff = false;
				return;
			}

			if (token.IsCancellationRequested)
				break;
			
			tr.position = Vector3.Lerp(startPos, endPos, normalizedTime);
			normalizedTime += speed * Time.deltaTime * 0.4f;
		}

		IsSteppingOn = false;
		
		// Use the elevator
		Elevator.Toggle(elevate);
	}

	private async UniTaskVoid stepOffPlatform(CancellationToken token)
	{
		var tr = PlatformUser.GetTransform();
		var startPos = tr.position;
		var endPos = startPos;
		var speed = PlatformUser.Data.Speed;
		
		switch (Elevator.State)
		{
			case EElevatorState.Elevated or EElevatorState.Elevating:
				endPos = UpperLink.position + Vector3.up * (PlatformUser.Agent.baseOffset * tr.localScale.y);
				break;
			case EElevatorState.Lowered or EElevatorState.Lowering:
				endPos = LowerLink.position + Vector3.up * (PlatformUser.Agent.baseOffset * tr.localScale.y);
				break;
		}

		var normalizedTime = 0.0f;
		while (normalizedTime < 1.0f)
		{
			await UniTask.NextFrame();
			
			if (this == null)
				return;
			
			if (PlatformUser == null || !PlatformUser.IsAlive)
			{
				PlatformUser = null;
				IsSteppingOn = false;
				IsSteppingOff = false;
				return;
			}

			if (token.IsCancellationRequested)
				break;
			
			tr.position = Vector3.Lerp(startPos, endPos, normalizedTime);
			normalizedTime += speed * Time.deltaTime * 0.4f;
		}
		
		// Finish action
		PlatformUser.Agent.CompleteOffMeshLink();
		PlatformUser.Agent.updateRotation = true;

		// Prevent weird rotation afterwards
		PlatformUser.Agent.Warp(endPos);
		PlatformUser.Agent.destination = PlatformUser.Destination;

		IsSteppingOff = false;
		PlatformUser = null;
	}
}