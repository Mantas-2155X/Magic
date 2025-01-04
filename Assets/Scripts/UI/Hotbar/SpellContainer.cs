using System.Globalization;
using Combat.Spells.Interfaces;
using Managers;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Hotbar
{
	public class SpellContainer : MonoBehaviour
	{
		[SerializeField]
		public Hotbar Hotbar;
		
		[SerializeField]
		public GameObject Selection;

		[SerializeField]
		public Image Icon;

		[SerializeField]
		public Transform Cooldown;
			
		[SerializeField]
		public TMP_Text Bind;

		[SerializeField]
		public TMP_Text Mana;
		
		public ISpell Spell { get; private set; }
		
		public void Update()
		{
			if (Spell == null)
				return;

			var scale = Cooldown.localScale;

			var finishTime = Spell.LastFinishedCast;
			if (finishTime > 0f)
			{
				var cooldownTime = finishTime + Spell.SpellData.Cooldown;

				var amount = MathTools.Remap(Time.time, finishTime, cooldownTime, 1f, 0f);
				amount = Mathf.Clamp01(amount);

				scale.x = amount;
			}
			else
			{
				scale.x = 0f;
			}
			
			Selection.SetActive(Spell.IsSelected);
			Cooldown.localScale = scale;
		}
		
		public void AssignSpell(ISpell spell)
		{
			Spell = spell;

			if (spell == null)
			{
				gameObject.SetActive(false);
				return;
			}
			
			gameObject.SetActive(true);
			
			var player = AIManager.Instance.Player;
			
			Selection.SetActive(spell == player.Spell);
			Cooldown.localScale = Vector3.one;
			Icon.sprite = spell.SpellData.Icon;
			Bind.text = player.GetHotbarKey(Hotbar.Containers.IndexOf(this));
			Mana.text = spell.SpellData.CastingCost.ToString(CultureInfo.CurrentCulture);
		}
	}
}