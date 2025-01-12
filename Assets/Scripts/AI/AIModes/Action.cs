using AI.Enums;
using AI.Interfaces;
using ScriptableObjects;
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

			// If low on resources, see if there's any spell that can be casted
			Owner.LowResources.UseResourceSpellIfNeeded();
			
			// Don't perform other actions if casting
			if (Owner.IsCasting)
				return;
			
			// If low on resources, see if there's anything that can be picked up
			if (Owner.ActionMode != EActionMode.UseSomething && Time.time >= Owner.ActionModes[EActionMode.UseSomething].LastExited + ((NPCData)Owner.Data).UseResourceEvery)
				Owner.LowResources.GrabResourceIfNeeded();

			var spells = Owner.Spells;
			
			// If there's multiple spells, switch them around when possible to eliminate idle time
			if (spells.Count > 1)
			{
				var primarySpell = Owner.Spells[0];
				var currentSpell = Owner.Spell;
				
				if (!primarySpell.IsSelected && !primarySpell.IsOnCooldown)
				{
					// Primary spell is no longer cooldown, switch back into it
					Owner.SelectSpell(primarySpell.SpellData);
				}
				else if (primarySpell.IsSelected && primarySpell.IsOnCooldown || !primarySpell.IsSelected && currentSpell.IsOnCooldown)
				{
					// Primary spell (or current one as well) is on cooldown, switch into another one
					var randomSpell = spells[Random.Range(1, spells.Count)];
					if (!randomSpell.IsOnCooldown && !randomSpell.SpellData.IsResource)
						Owner.SelectSpell(randomSpell.SpellData);
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
		
		public void AggressiveChanged(bool previousAggressive, bool newAggressive)
		{
			
		}
		
		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}
	}
}