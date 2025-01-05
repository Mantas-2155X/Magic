using System;
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
		public static Hotbar Instance;
		
		[SerializeField]
		public Transform Template;
		
		[SerializeField]
		public TMP_Text SelectedSpell;

		[SerializeField]
		public int Size = 7;
		
		[NonSerialized]
		public readonly List<SpellContainer> Containers = new ();

		public void Awake()
		{
			Instance = this;
			
			BaseAlive.OnSpellSelectedEvent.AddListener(onSpellSelected);
		}

		public void SetupHotbar()
		{
			if (Size > Containers.Count)
			{
				var toCreate = Size - Containers.Count;
				if (toCreate > 0)
				{
					var parent = Template.parent;
					
					for (var i = 0; i < Size; i++)
					{
						var copy = Instantiate(Template.gameObject, parent);
						copy.name = $"Container {i}";
				
						var container = copy.GetComponent<SpellContainer>();
						Containers.Add(container);
					}
				}
			}
			
			UpdateHotbar();
		}

		public void UpdateHotbar()
		{
			var player = AIManager.Instance.Player;
			var spellCount = player.Spells.Count;
			
			for (var i = 0; i < Containers.Count; i++)
			{
				ISpell spell = null;

				if (i < spellCount)
					spell = player.Spells[i];
				
				Containers[i].AssignSpell(spell, i);
			}
		}
		
		public void OnSpawn()
		{
			SetupHotbar();
		}
		
		public void OnDeath()
		{
			for (var i = 0; i < Containers.Count; i++)
				Containers[i].AssignSpell(null, i);
		}

		private void onSpellSelected(IAlive alive, ISpell previousSpell, ISpell newSpell)
		{
			if (alive is not Player)
				return;

			SelectedSpell.text = newSpell == null ? "" : newSpell.SpellData.Name;
		}
	}
}