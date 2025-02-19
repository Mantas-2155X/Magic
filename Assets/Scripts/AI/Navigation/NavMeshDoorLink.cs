using System.Collections.Generic;
using AI.Enums;
using Managers;
using Objects.Base;
using Tools;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace AI.Navigation
{
	public class NavMeshDoorLink : MonoBehaviour
	{
		[SerializeField]
		public NavMeshLink[] Links;
	
		[SerializeField]
		public BaseDoor Door;

		[SerializeField]
		public BaseButton[] Buttons;
	
		public bool IsPartial { get; private set; }

		public NPC User { get; private set; }
	
		private readonly List<NPC> linkUsers = new ();
	
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (User == null)
				return;

			// Dying or getting interrupted should clear the user to prevent a lock
			if (User.IsAlive && User.ActionMode == EActionMode.Use)
				return;

			User = null;
		}
	
		public void OnDoorOpened()
		{
			User = null;
			IsPartial = false;
			toggleLinks(false);
		}

		public void OnDoorClosed()
		{
			User = null;
			IsPartial = false;
			toggleLinks(true);
		}
	
		public void OnDoorOpening()
		{
			IsPartial = true;
			toggleLinks(true);
		}
	
		public void OnDoorClosing()
		{
			IsPartial = true;
			toggleLinks(true);
		}

		public bool TryOpen(NPC user)
		{
			if (user.ActionMode == EActionMode.Use)
				return false;

			User = user;
		
			switch (Buttons.Length)
			{
				// No buttons, use the door
				case 0: 
					user.Use(Door, user.Destination);
					break;
				// One button, use it
				case 1: 
					user.Use(Buttons[0], user.Destination);
					break;
				// Multiple buttons, use the cheapest one
				default: 
				{
					var position = user.GetTransform().position;
			
					var cheapestPath = float.MaxValue;
					var pathIndex = 0;
				
					for (var i = 0; i < Buttons.Length; i++)
					{
						var buttonPosition = Buttons[i].GetTransform().position;
						var path = new NavMeshPath();
					
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
				
					user.Use(Buttons[pathIndex], user.Destination);
					break;
				}
			}
		
			return true;
		}

		private void toggleLinks(bool state)
		{
			linkUsers.Clear();

			for (var i = 0; i < Links.Length; i++)
			{
				var link = Links[i];
			
				if (!state && link.occupied)
				{
					foreach (var npc in AIManager.Instance.NPCs)
					{
						var agent = npc.Agent;
				
						if (!npc.IsAlive || !agent.enabled || !agent.isOnOffMeshLink || agent.currentOffMeshLinkData.owner != link)
							continue;

						linkUsers.Add(npc);
					}
				}
		
				link.enabled = state;

				// Cancel the isOnOffMeshLink check
				for (var k = 0; k < linkUsers.Count; k++)
				{
					var linkUser = linkUsers[k];
					linkUser.Agent.Warp(linkUser.GetTransform().position);
					linkUser.Agent.destination = linkUser.Destination;
				}
			}
		}
	}
}