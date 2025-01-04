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

			// Don't perform other actions if casting
			if (Owner.IsCasting)
				return;
			
			// If low on resources, see if there's anything that can be picked up
			if (Owner.ActionMode != EActionMode.UseSomething)
				Owner.LowResources.GrabResourceIfNeeded();

			var spells = Owner.Spells;
			
			// If there's multiple spells, switch them around when possible to eliminate idle time
			if (spells.Count > 1)
			{
				var primarySpell = Owner.Spells[0];
				if (primarySpell.IsSelected)
				{
					// Primary spell is on cooldown, try switching into another one if it isn't on cooldown
					if (primarySpell.IsOnCooldown)
					{
						var randomSpell = spells[Random.Range(1, spells.Count)];
						if (!randomSpell.IsOnCooldown)
							Owner.SelectSpell(randomSpell.SpellData);
					}
				}
				else
				{
					// Primary spell is no longer cooldown, switch back into it
					if (!primarySpell.IsOnCooldown)
						Owner.SelectSpell(primarySpell.SpellData);
				}
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