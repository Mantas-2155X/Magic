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
		public Transform Template;
		
		[SerializeField]
		public TMP_Text SelectedSpell;

		[SerializeField]
		public int Size = 7;
		
		private readonly List<SpellContainer> containers = new ();

		public void Awake()
		{
			for (var i = 0; i < Size; i++)
			{
				var copy = Instantiate(Template.gameObject, Template.parent);
				copy.name = $"Container {i}";
				
				var container = copy.GetComponent<SpellContainer>();
				containers.Add(container);
			}
			
			BaseAlive.OnSpellSelectedEvent.AddListener(onSpellSelected);
		}

		public int GetContainerIndex(SpellContainer container)
		{
			return containers.IndexOf(container);
		}
		
		public void OnSpawn()
		{
			var player = AIManager.Instance.Player;
			var spellCount = player.Spells.Count;
			
			for (var i = 0; i < containers.Count; i++)
			{
				ISpell spell = null;

				if (i < spellCount)
					spell = player.Spells[i];
				
				containers[i].AssignSpell(spell);
			}
		}
		
		public void OnDeath()
		{
			for (var i = 0; i < containers.Count; i++)
				containers[i].AssignSpell(null);
		}

		private void onSpellSelected(IAlive alive, ISpell previousSpell, ISpell newSpell)
		{
			if (alive is not Player)
				return;

			SelectedSpell.text = newSpell == null ? "" : newSpell.SpellData.Name;
		}
	}
}