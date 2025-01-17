using System;
using System.Collections.Generic;
using Combat.Spells.Interfaces;
using Managers;
using UnityEngine;

namespace UI.Spellbook
{
	public class Spellbook : MonoBehaviour
	{
		public static Spellbook Instance;

		[SerializeField]
		public Transform Template;

		[NonSerialized]
		public readonly List<SpellContainer> Containers = new ();

		public void Awake()
		{
			Instance = this;
		}

		public void OnDisable()
		{
			for (var i = 0; i < Containers.Count; i++)
			{
				var container = Containers[i];
				container.OnEndDrag(null);
			}
		}

		public void Toggle()
		{
			Display(!isActiveAndEnabled);
		}
		
		public void Display(bool state)
		{
			if (state == isActiveAndEnabled)
				return;
			
			var player = AIManager.Instance.Player;

			if (state)
			{
				// Don't show if we don't have any spells
				if (player.Spells.Count == 0)
					return;
				
				player.DisableInput(false);
				UpdateSpellbook();
			}
			else
			{
				player.EnableInput();
			}

			gameObject.SetActive(state);
		}
		
		public void UpdateSpellbook()
		{
			var player = AIManager.Instance.Player;

			var spells = player.Spells;
			var spellCount = spells.Count;
			
			if (spellCount > Containers.Count)
			{
				var toCreate = spellCount - Containers.Count;
				if (toCreate > 0)
				{
					var parent = Template.parent;
					
					for (var i = 0; i < toCreate; i++)
					{
						var copy = Instantiate(Template.gameObject, parent);
						copy.name = $"Container {i}";
				
						var container = copy.GetComponent<SpellContainer>();
						Containers.Add(container);
					}
				}
			}
			
			for (var i = 0; i < Containers.Count; i++)
			{
				ISpell spell = null;

				if (i < spellCount)
					spell = spells[i];
				
				Containers[i].AssignSpell(spell, i);
			}
		}

		public void OnCloseClicked()
		{
			Display(false);
		}
	}
}