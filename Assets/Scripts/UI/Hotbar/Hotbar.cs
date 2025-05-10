using System;
using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Interfaces;
using Combat.Spells.Interfaces;
using Managers;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hotbar
{
	public class Hotbar : MonoBehaviour
	{
		[SerializeField]
		public Transform Template;
		
		[SerializeField]
		public TMP_Text SelectedSpell;

		[SerializeField]
		public Localizer SelectedSpellLocalizer;
		
		[SerializeField]
		public Image Background;
		
		[SerializeField]
		public Color CanCastColor;
		
		[SerializeField]
		public Color CantCastColor;

		[SerializeField]
		public int Size = 7;
		
		[NonSerialized]
		public readonly List<SpellContainer> Containers = new ();

		public void Awake()
		{
			BaseAlive.OnSpellSelectedEvent.AddListener(onSpellSelected);
		}

		public void OnDestroy()
		{
			BaseAlive.OnSpellSelectedEvent.RemoveListener(onSpellSelected);
		}

		public void UpdateHotbar()
		{
			var player = AIManager.Instance.Player;
			var spellCount = player.Spells.Count;

			SelectedSpell.gameObject.SetActive(spellCount > 0);
			Background.enabled = spellCount > 0;

			onSpellSelected(player, null, player.Spell);
			
			if (Size > Containers.Count)
			{
				var toCreate = Size - Containers.Count;
				if (toCreate > 0)
				{
					var parent = Template.parent;
					
					for (var i = 0; i < Size; i++)
					{
						var copy = Instantiate(Template.gameObject, parent);
						copy.name = $"Container {Containers.Count}";
				
						var container = copy.GetComponent<SpellContainer>();
						Containers.Add(container);
					}
				}
			}
			
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
			UpdateHotbar();
		}
		
		public void OnDeath()
		{
			for (var i = 0; i < Containers.Count; i++)
				Containers[i].AssignSpell(null, i);
		}

		private void onSpellSelected(IAlive alive, ISpell previousSpell, ISpell newSpell)
		{
			if (alive is not AI.Player)
				return;

			if (newSpell.IsNull())
			{
				SelectedSpellLocalizer.Key = "";
				SelectedSpellLocalizer.Text.text = "";
			}
			else
			{
				SelectedSpellLocalizer.Key = newSpell.SpellData.Name;
				SelectedSpellLocalizer.Apply();
			}
		}
	}
}