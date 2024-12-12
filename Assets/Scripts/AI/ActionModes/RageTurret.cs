using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class RageTurret : IActionMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			if (Owner.AIMode != EAIMode.Action)
				return;
			
			Owner.Spin.SpinEndlessly(Owner.transform);
			
			Owner.Weapon?.StartCasting();
			Owner.Weapon?.FinishCasting();
		}

		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}