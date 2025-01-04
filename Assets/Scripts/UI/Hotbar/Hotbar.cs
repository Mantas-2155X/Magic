using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Interfaces;
using Combat.Spells.Interfaces;
using Managers;
using TMPro;
using UnityEngine;

namespace UI.Hotbar
{
	public class Hotbar : MonoBehaviour
	{
		[SerializeField]
		public List<SpellContainer> Containers;
		
		[SerializeField]
		public TMP_Text SelectedSpell;

		public void Awake()
		{
			BaseAlive.OnSpellSelectedEvent.AddListener(onSpellSelected);
		}

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

		private void onSpellSelected(IAlive alive, ISpell previousSpell, ISpell newSpell)
		{
			if (alive is not Player)
				return;

			SelectedSpell.text = newSpell == null ? "" : newSpell.SpellData.Name;
		}
	}
}