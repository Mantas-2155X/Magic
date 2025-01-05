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

		private readonly List<SpellContainer> containers = new ();

		public void Awake()
		{
			Instance = this;
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
			player.EnableInput();

			if (state)
			{
				player.DisableInput();
				SetupSpellbook();
			}

			gameObject.SetActive(state);
		}
		
		public void SetupSpellbook()
		{
			var player = AIManager.Instance.Player;

			var spellCount = player.Spells.Count;
			if (spellCount > containers.Count)
			{
				var toCreate = spellCount - containers.Count;
				if (toCreate > 0)
				{
					var parent = Template.parent;
					
					for (var i = 0; i < toCreate; i++)
					{
						var copy = Instantiate(Template.gameObject, parent);
						copy.name = $"Container {i}";
				
						var container = copy.GetComponent<SpellContainer>();
						containers.Add(container);
					}
				}
			}
			
			UpdateSpellbook();
		}

		public void UpdateSpellbook()
		{
			var player = AIManager.Instance.Player;

			var spells = player.Spells;
			var spellCount = spells.Count;
			
			for (var i = 0; i < containers.Count; i++)
			{
				ISpell spell = null;

				if (i < spellCount)
					spell = spells[i];
				
				containers[i].AssignSpell(spell, i);
			}
		}

		public void OnCloseClicked()
		{
			Display(false);
		}
	}
}