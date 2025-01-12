using AI.Enums;
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
		
		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget);
		
		public void OtherTargetChanged(Component previousOtherTarget, Component newOtherTarget);
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination);
		
		public void AggressiveChanged(bool previousAggressive, bool newAggressive);

		public void CommunicationReceived(ECommunication type, NPC source, object data);
	}
}