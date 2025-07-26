using System.Globalization;
using Combat.Spells.Interfaces;
using Managers;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Spellbook
{
	public class SpellContainer : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, ISubmitHandler
	{
		[SerializeField]
		public GameObject Hover;

		[SerializeField]
		public Image Icon;
			
		[SerializeField]
		public TMP_Text Bind;

		[SerializeField]
		public TMP_Text Mana;

		[SerializeField]
		public Button Button;
		
		public ISpell Spell { get; private set; }
		public int Index { get; private set; }

		private bool dragging;
		private int newSpellIndex;
		private int currentTransformIndex;
		
		public void AssignSpell(ISpell spell, int index)
		{
			Spell = spell;
			Index = index;

			if (spell.IsNull())
			{
				gameObject.SetActive(false);
				return;
			}
			
			gameObject.SetActive(true);
			
			Hover.SetActive(false);
			Icon.sprite = spell.SpellData.Icon;
			Mana.text = spell.SpellData.CastingCost.ToString(CultureInfo.CurrentCulture);
			
			if (index > Player.Instance.HUD.Hotbar.Size)
			{
				Bind.gameObject.SetActive(false);
				return;
			}
			
			Bind.text = AIManager.Instance.Player.GetHotbarKey(index);
			Bind.gameObject.SetActive(true);
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			dragging = true;
			newSpellIndex = Index;
			currentTransformIndex = transform.GetSiblingIndex();
			
			SetInfo();
		}
		
		public void OnDrag(PointerEventData eventData)
		{
			var pos = eventData.position;
			var containers = Player.Instance.HUD.Spellbook.Containers;

			for (var i = 0; i < containers.Count; i++)
			{
				var container = containers[i];
				var rect = (RectTransform)container.transform;
				
				if (!RectTransformUtility.RectangleContainsScreenPoint(rect, pos))
					continue;

				newSpellIndex = rect.GetSiblingIndex() - 1;
				transform.SetSiblingIndex(newSpellIndex + 1);
				break;
			}
		}
		
		public void OnEndDrag(PointerEventData eventData)
		{
			if (!dragging)
				return;
			
			dragging = false;
			transform.SetSiblingIndex(currentTransformIndex);
			AIManager.Instance.Player.SetSpellIndex(Spell.SpellData, newSpellIndex);
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (dragging)
				return;
			
			Hover.SetActive(true);
			
			SetInfo();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (dragging)
				return;
			
			Hover.SetActive(false);
		}
		
		public void OnSelect(BaseEventData eventData)
		{
			if (eventData is not AxisEventData)
				return;
			
			var spellbook = Player.Instance.HUD.Spellbook;
			spellbook.ScrollRect.ScrollToCenter((RectTransform)transform, spellbook);
			
			SetInfo();
		}

		public void OnSubmit(BaseEventData eventData)
		{
			if (eventData is PointerEventData)
				return;

			Player.Instance.HUD.Spellbook.GrabContainer(this);
			
			SetInfo();
		}

		public void SetInfo()
		{
			var spellbook = Player.Instance.HUD.Spellbook;

			spellbook.InfoName.Key = Spell.SpellData.Name;
			spellbook.InfoName.Apply();
			
			spellbook.InfoDescription.Key = Spell.SpellData.Description;
			spellbook.InfoDescription.Apply();
		}
	}
}