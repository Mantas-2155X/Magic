using System;
using System.Collections.Generic;
using Combat.Spells.Interfaces;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Spellbook
{
	public class Spellbook : MonoBehaviour
	{
		[SerializeField]
		public Transform Template;

		[SerializeField]
		public GridLayoutGroup GridLayoutGroup;
		
		[SerializeField]
		public Button CloseButton;
		
		[SerializeField]
		public ScrollRect ScrollRect;
		
		[NonSerialized]
		public readonly List<SpellContainer> Containers = new ();

		private SpellContainer grabbedContainer;

		/// <summary>
		/// Used to move the containers via kb/ctrl
		/// </summary>
		public void GrabContainer(SpellContainer container)
		{
			var previousContainer = grabbedContainer;
			if (previousContainer != null)
				previousContainer.Icon.color = Color.white;
			
			grabbedContainer = container;
			if (grabbedContainer != null)
				grabbedContainer.Icon.color = new Color(0.75f, 0.75f, 0.75f);

			if (previousContainer == null || grabbedContainer == null)
				return;

			var spell = previousContainer.Spell;
			if (spell != null)
			{
				// Move previously grabbed container to the newly grabbed containers index
				AIManager.Instance.Player.SetSpellIndex(spell.SpellData, grabbedContainer.Index);
			}
				
			grabbedContainer.Icon.color = Color.white;
			grabbedContainer = null;
		}
		
		public void OnDisable()
		{
			for (var i = 0; i < Containers.Count; i++)
			{
				var container = Containers[i];
				container.OnEndDrag(null);
			}
			
			GrabContainer(null);
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

			if (state)
			{
				updateNavigation();
				SelectionManager.Instance.SetSelection(Containers[0].gameObject);
			}
			else
			{
				SelectionManager.Instance.SetSelection(null);
			}
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
					spell = spells[i];
				
				Containers[i].AssignSpell(spell, i);
			}
			
			if (isActiveAndEnabled)
				updateNavigation();
		}

		public void OnCloseClicked()
		{
			Display(false);
		}

		private void updateNavigation()
		{
			var spellCount = AIManager.Instance.Player.Spells.Count;
			
			var firstContainer = Containers[0];
			var lastContainer = Containers[spellCount - 1];

			var constraints = GridLayoutGroup.constraintCount;
			
			CloseButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnDown = firstContainer.Button,
				selectOnUp = lastContainer.Button
			};

			for (var i = 0; i < spellCount; i++)
			{
				var container = Containers[i];

				var nav = new Navigation
				{
					mode = Navigation.Mode.Explicit
				};

				SpellContainer previousContainer;
				SpellContainer nextContainer;

				if (i == 0)
				{
					previousContainer = lastContainer;
					nextContainer = spellCount - 1 > 1 ? Containers[i + 1] : container;
				}
				else if (i == spellCount - 1)
				{
					previousContainer = spellCount - 1 > 1 ? Containers[i - 1] : container;
					nextContainer = firstContainer;
				}
				else
				{
					previousContainer = Containers[i - 1];
					nextContainer = Containers[i + 1];
				}
				
				if (container == firstContainer)
				{
					nav.selectOnLeft = lastContainer.Button;
					nav.selectOnRight = nextContainer.Button;
				}
				else if (container == lastContainer)
				{
					nav.selectOnLeft = previousContainer.Button;
					nav.selectOnRight = firstContainer.Button;
				}
				else
				{
					nav.selectOnLeft = previousContainer.Button;
					nav.selectOnRight = nextContainer.Button;
				}

				Button aboveButton;
				Button belowButton;

				var aboveIndex = i - constraints;
				if (aboveIndex < 0)
				{
					aboveButton = CloseButton;
				}
				else
				{
					aboveButton = Containers[aboveIndex].Button;
				}
				
				var belowIndex = i + constraints;
				if (belowIndex >= spellCount)
				{
					if (aboveButton != CloseButton)
					{
						belowButton = CloseButton;
					}
					else
					{
						belowButton = lastContainer.Button;
					}
				}
				else
				{
					belowButton = Containers[belowIndex].Button;
				}

				nav.selectOnUp = aboveButton;
				nav.selectOnDown = belowButton;
				
				container.Button.navigation = nav;
			}
		}
	}
}