using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.AIModes
{
	public class Action : IAIMode
	{
		public NPC Owner { get; set; }
		
		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;
			
			Owner.Chase.ResetChaseRange(true);
		}
		
		public void Disabled()
		{
			Owner = null;
			LastExited = Time.time;
		}
		
		public void Update()
		{
			if (Owner.AIMode != EAIMode.Action)
				return;
			
			// If low on resources, see if there's anything that can be picked up
			if (Owner.ActionMode != EActionMode.UseSomething && !Owner.IsCasting)
			{
				Owner.LowResources.GrabResourceIfNeeded();
			}
		}

		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
		{
			
		}

		public void OtherTargetChanged(Component previousOtherTarget, Component newOtherTarget)
		{
			
		}
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
		
		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}
	}
}