using System.Collections.Generic;
using AI;
using AI.ActionModes;
using AI.Enums;
using Managers;
using Objects.Base;
using Tools;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshDoorLink : MonoBehaviour
{
	[SerializeField]
	public NavMeshLink Link;
	
	[SerializeField]
	public BaseDoor Door;

	[SerializeField]
	public BaseButton[] Buttons;
	
	public bool IsPartial { get; private set; }

	public NPC User { get; private set; }
	
	private readonly List<NPC> linkUsers = new ();
	private readonly List<NavMeshPath> buttonPaths = new ();
	
	public void OnDoorOpened()
	{
		User = null;
		IsPartial = false;
		toggleLink(false);
	}

	public void OnDoorClosed()
	{
		User = null;
		IsPartial = false;
		toggleLink(true);
	}
	
	public void OnDoorOpening()
	{
		IsPartial = true;
		toggleLink(true);
	}
	
	public void OnDoorClosing()
	{
		IsPartial = true;
		toggleLink(true);
	}

	public bool TryOpen(NPC user)
	{
		if (user.ActionMode == EActionMode.UseSomething)
			return false;

		User = user;
		
		var actionMode = (UseSomething)user.ActionModes[EActionMode.UseSomething];
		actionMode.WalkAfterwards = user.Destination;
		
		switch (Buttons.Length)
		{
			// No buttons, use the door
			case 0: 
				user.UseSomething(Door);
				break;
			// One button, use it
			case 1: 
				user.UseSomething(Buttons[0]);
				break;
			// Multiple buttons, use the cheapest one
			default: 
			{
				buttonPaths.Clear();
				
				var position = user.GetTransform().position;
			
				var cheapestPath = float.MaxValue;
				var pathIndex = 0;
				
				for (var i = 0; i < Buttons.Length; i++)
				{
					var buttonPosition = Buttons[i].GetTransform().position;
					var path = new NavMeshPath();
					
					buttonPaths.Add(path);
					
					if (!NavMesh.CalculatePath(position, buttonPosition, NavMeshTools.GetAreaMask(), path))
						continue;
					
					if (path.status is NavMeshPathStatus.PathInvalid or NavMeshPathStatus.PathPartial)
						continue;

					var cost = path.Cost();
					if (cost < cheapestPath)
					{
						cheapestPath = cost;
						pathIndex = i;
					}
				}
				
				user.UseSomething(Buttons[pathIndex]);
				break;
			}
		}
		
		return true;
	}
	
	private void toggleLink(bool state)
	{
		linkUsers.Clear();
		
		if (!state && Link.occupied)
		{
			foreach (var npc in AIManager.Instance.NPCs)
			{
				var agent = npc.Agent;
				
				if (!npc.IsAlive || !agent.enabled || !agent.isOnOffMeshLink || agent.currentOffMeshLinkData.owner != Link)
					continue;

				linkUsers.Add(npc);
			}
		}
		
		Link.enabled = state;

		// Cancel the isOnOffMeshLink check
		for (var i = 0; i < linkUsers.Count; i++)
		{
			var linkUser = linkUsers[i];
			linkUser.Agent.Warp(linkUser.GetTransform().position);
			linkUser.Agent.destination = linkUser.Destination;
		}
	}
}