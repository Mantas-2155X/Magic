using System.Globalization;
using Combat.Spells.Interfaces;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Spellbook
{
	public class SpellContainer : MonoBehaviour
	{
		[SerializeField]
		public Image Icon;
			
		[SerializeField]
		public TMP_Text Bind;

		[SerializeField]
		public TMP_Text Mana;
		
		public ISpell Spell { get; private set; }
		public int Index { get; private set; }

		public void AssignSpell(ISpell spell, int index)
		{
			Spell = spell;
			Index = index;

			if (spell == null)
			{
				gameObject.SetActive(false);
				return;
			}
			
			gameObject.SetActive(true);
			
			Icon.sprite = spell.SpellData.Icon;
			Mana.text = spell.SpellData.CastingCost.ToString(CultureInfo.CurrentCulture);
			
			if (index > Hotbar.Hotbar.Instance.Size)
			{
				Bind.gameObject.SetActive(false);
				return;
			}
			
			Bind.text = AIManager.Instance.Player.GetHotbarKey(index);
			Bind.gameObject.SetActive(true);
		}
	}
}