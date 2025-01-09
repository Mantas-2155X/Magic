using System.Collections.Generic;
using AI;
using AI.ActionModes;
using AI.Enums;
using Managers;
using Objects.Base;
using Unity.AI.Navigation;
using UnityEngine;

public class NavMeshDoorLink : MonoBehaviour
{
	[SerializeField]
	public NavMeshLink Link;
	
	[SerializeField]
	public BaseDoor Door;

	public bool IsPartial { get; private set; }

	public NPC User { get; private set; }
	
	private readonly List<NPC> linkUsers = new ();
	
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
		
		user.UseSomething(Door);
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