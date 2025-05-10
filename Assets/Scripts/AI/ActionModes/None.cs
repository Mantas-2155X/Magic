using AI.Enums;
using AI.Interfaces;
using State.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class None : IActionMode
	{
		public NPC Owner { get; set; }
		
		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }

		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;
		}
		
		public void Disabled()
		{
			Owner = null;
			LastExited = Time.time;
		}
		
		public void Update()
		{

		}

		public void AttackTargetChanged(IIdentifiable previousAttackTarget, IIdentifiable newAttackTarget)
		{

		}
		
		public void OtherTargetChanged(IIdentifiable previousOtherTarget, IIdentifiable newOtherTarget)
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