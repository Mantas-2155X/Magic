using System.Threading;
using AI;
using AI.ActionModes;
using AI.Enums;
using Cysharp.Threading.Tasks;
using Objects.Base;
using Objects.Enums;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshElevatorLink : MonoBehaviour
{
	[SerializeField]
	public NavMeshLink Link;

	[SerializeField]
	public BaseElevator Elevator;
	
	[SerializeField]
	public BaseButton ElevateButton;
	
	[SerializeField]
	public BaseButton LowerButton;

	[SerializeField]
	public Transform StepTarget;
	
	public bool IsPartial { get; private set; }
	
	public NPC ButtonUser { get; private set; }
	
	public NPC PlatformUser { get; private set; }

	public bool IsFacing { get; private set; }

	public bool IsSteppingOn { get; private set; }
	
	public bool IsSteppingOff { get; private set; }
	
	private CancellationTokenSource cancellationToken = new ();
	
	public void Update()
	{
		if (ButtonUser != null)
		{
			// Dying or getting interrupted should clear the user to prevent a lock
			if (ButtonUser.IsAlive && ButtonUser.ActionMode == EActionMode.UseSomething)
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
				// Look at the finish position while it's moving
				switch (Elevator.State)
				{
					case EElevatorState.Elevated or EElevatorState.Elevating:
						IsFacing = PlatformUser.AimAt.AimTowards(Link.endTransform);
						break;
					case EElevatorState.Lowered or EElevatorState.Lowering:
						IsFacing = PlatformUser.AimAt.AimTowards(Link.startTransform);
						break;
				}
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

	public bool TryUse(NPC user, bool elevate)
	{
		if (user.ActionMode == EActionMode.UseSomething)
			return false;

		ButtonUser = user;
		
		var actionMode = (UseSomething)user.ActionModes[EActionMode.UseSomething];
		actionMode.WalkAfterwards = user.Destination;

		user.UseSomething(elevate ? ElevateButton : LowerButton);
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
		if (IsSteppingOff || !IsFacing)
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
		var endPos = StepTarget.position + Vector3.up * PlatformUser.Agent.baseOffset;
		var speed = PlatformUser.Data.Speed;
		
		var normalizedTime = 0.0f;
		while (normalizedTime < 1.0f)
		{
			await UniTask.NextFrame();
			
			if (PlatformUser == null || !PlatformUser.IsAlive)
			{
				IsSteppingOn = false;
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
				endPos = Link.endTransform.position + Vector3.up * PlatformUser.Agent.baseOffset;
				break;
			case EElevatorState.Lowered or EElevatorState.Lowering:
				endPos = Link.startTransform.position + Vector3.up * PlatformUser.Agent.baseOffset;
				break;
		}

		var normalizedTime = 0.0f;
		while (normalizedTime < 1.0f)
		{
			await UniTask.NextFrame();
			
			if (PlatformUser == null || !PlatformUser.IsAlive)
			{
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