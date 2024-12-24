using AI.Enums;
using UnityEngine;

namespace AI.Interfaces
{
	public interface IActionMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner);
		
		public void Disabled();
		
		public void Update();
		
		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget);
		
		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination);

		public void CommunicationReceived(ECommunication type, NPC source, object data);
	}
}