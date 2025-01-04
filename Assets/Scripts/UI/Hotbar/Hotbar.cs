using System.Collections.Generic;
using Combat.Spells.Interfaces;
using Managers;
using UnityEngine;

namespace UI.Hotbar
{
	public class Hotbar : MonoBehaviour
	{
		[SerializeField]
		public List<SpellContainer> Containers;
		
		public void OnSpawn()
		{
			var player = AIManager.Instance.Player;
			var spellCount = player.Spells.Count;
			
			for (var i = 0; i < Containers.Count; i++)
			{
				ISpell spell = null;

				if (i < spellCount)
					spell = player.Spells[i];
				
				Containers[i].AssignSpell(spell);
			}
		}
		
		public void OnDeath()
		{
			for (var i = 0; i < Containers.Count; i++)
				Containers[i].AssignSpell(null);
		}
	}
}