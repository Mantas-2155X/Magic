using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Enums;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Base;
using State.Enums;
using State.Interfaces;
using Tools;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

namespace AI.Navigation
{
	public class NavMeshDoorLink : MonoBehaviour, ISaveable
	{
		[SerializeField]
		public NavMeshLink[] Links;
	
		[SerializeField]
		public BaseDoor Door;

		[SerializeField]
		public BaseButton[] Buttons;
	
		public bool IsPartial { get; private set; }

		public NPC User { get; private set; }
	
		public List<NPC> LinkUsers { get; private set; } = new ();
	
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
	
		#region Identify / SaveLoad

		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => false;
		
		public virtual bool ExternallySpawned { get; set; } = false;

		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
		public virtual ELoadType LoadType => ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.VeryLate;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public virtual JObject GetCreation()
		{
			throw new NotImplementedException();
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(NavMeshDoorLink).ToString()] = JObject.FromObject(new NavMeshDoorLinkState(this));
			
			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(NavMeshDoorLink).ToString(), out var navMeshDoorLinkState) && navMeshDoorLinkState != null)
				navMeshDoorLinkState.ToObject<NavMeshDoorLinkState>().Apply(this);
		}

		public void SetState(List<string> linkUsersAlivesIDs, string userObjectID, bool partial)
		{
			var stateManager = StateManager.Instance;
			
			LinkUsers.Clear();
			
			for (var i = 0; i < linkUsersAlivesIDs.Count; i++)
			{
				var linkUserIdentifiable = stateManager.GetRegisteredObject(linkUsersAlivesIDs[i]);
				if (linkUserIdentifiable.IsNull() || linkUserIdentifiable is not NPC linkUser)
					continue;
				
				LinkUsers.Add(linkUser);
			}

			var userIdentifiable = stateManager.GetRegisteredObject(userObjectID);
			if (userIdentifiable.NotNull() && userIdentifiable is NPC user)
				User = user;
			
			IsPartial = partial;
		}
		
		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
	
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}
		
		private void toggleLinks(bool state)
		{
			LinkUsers.Clear();

			for (var i = 0; i < Links.Length; i++)
			{
				var link = Links[i];
			
				if (!state && link.occupied)
				{
					foreach (var npc in AIManager.Instance.NPCs)
					{
						var agent = npc.Agent;
						if (!agent.IsNavMesh)
							continue;
				
						if (!npc.IsAlive || !agent.NavMeshAgent.enabled || !agent.IsOnOffMeshLink || agent.CurrentOffMeshLinkData.owner != link)
							continue;

						LinkUsers.Add(npc);
					}
				}
		
				link.enabled = state;

				// Cancel the isOnOffMeshLink check
				for (var k = 0; k < LinkUsers.Count; k++)
				{
					var linkUser = LinkUsers[k];
					linkUser.Agent.Warp(linkUser.GetTransform().position);
					linkUser.Agent.Destination = linkUser.Destination;
				}
			}
		}
		
		[JsonObject]
		public class NavMeshDoorLinkState : IState
		{
			[JsonProperty]
			public List<string> LinkUsersObjectIDs;

			[JsonProperty]
			public string UserObjectID;

			[JsonProperty]
			public bool Partial;
	
			public NavMeshDoorLinkState() { }
			
			public NavMeshDoorLinkState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not NavMeshDoorLink navMeshDoorLink)
					return;
				
				Partial = navMeshDoorLink.IsPartial;
				UserObjectID = navMeshDoorLink.User != null ? navMeshDoorLink.User.ObjectID : null;
				LinkUsersObjectIDs = new List<string>();

				for (var i = 0; i < navMeshDoorLink.LinkUsers.Count; i++)
				{
					var npc = navMeshDoorLink.LinkUsers[i];
					if (npc.IsNull() || !npc.IsAlive)
						continue;

					LinkUsersObjectIDs.Add(npc.ObjectID);
				}
			}
			
			public void Apply(object obj)
			{
				if (obj is not NavMeshDoorLink navMeshDoorLink)
					return;

				navMeshDoorLink.SetState(LinkUsersObjectIDs, UserObjectID, Partial);
			}
		}
	}
}