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
			
		}

		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
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