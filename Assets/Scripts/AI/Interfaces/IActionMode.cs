using AI.Enums;
using State.Interfaces;
using UnityEngine;

namespace AI.Interfaces
{
	public interface IActionMode
	{
		public NPC Owner { get; set; }
		
		public float LastEntered { get; }
		public float LastExited { get; }

		public void Enabled(NPC owner);
		
		public void Disabled();
		
		public void Update();
		
		public void AttackTargetChanged(IIdentifiable previousAttackTarget, IIdentifiable newAttackTarget);
		
		public void OtherTargetChanged(IIdentifiable previousOtherTarget, IIdentifiable newOtherTarget);
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination);
		
		public void CommunicationReceived(ECommunication type, NPC source, object data);
	}
}