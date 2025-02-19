using AI;
using AI.Base;
using AI.Interfaces;
using Managers;
using Objects;
using Objects.Interfaces;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class HUD : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text LookTarget;
		
		[SerializeField]
		public Localizer LookTargetLocalizer;

		[SerializeField]
		public Image Crosshair;
		
		[SerializeField]
		public Image Cast;

		[SerializeField]
		public Image Cooldown;
		
		[SerializeField]
		public Spellbook.Spellbook Spellbook;

		[SerializeField]
		public Hotbar.Hotbar Hotbar;

		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnDestroy()
		{
			BaseAlive.OnDeathEvent.RemoveListener(OnDeath);
			BaseAlive.OnSpawnEvent.RemoveListener(OnSpawn);
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (LookTarget.text != "")
				LookTarget.text = "";
			
			var player = AIManager.Instance.Player;
			if (player == null)
				return;

			var spell = player.Spell;
			if (spell != null)
			{
				if (spell.IsCasting)
				{
					var amount = MathTools.Remap(Time.time, spell.LastStartedCast, spell.PredictFinishCast, 0f, 1f);
					amount = Mathf.Clamp01(amount);
					
					Cast.fillAmount = amount;
				}
				else
				{
					Cast.fillAmount = 0f;
				}

				if (spell.LastFinishedCast < 0)
				{
					Cooldown.fillAmount = 0f;
				}
				else
				{
					var finishTime = spell.LastFinishedCast;
					var cooldownTime = finishTime + spell.SpellData.Cooldown;

					var amount = MathTools.Remap(Time.time, finishTime, cooldownTime, 1f, 0f);
					amount = Mathf.Clamp01(amount);
					
					Cooldown.fillAmount = amount;
				}
			}
			else
			{
				Cast.fillAmount = 0f;
				Cooldown.fillAmount = 0f;
			}
			
			Crosshair.color = player.IsGrounded() ? Color.white : Color.red;
			
			if (Physics.Raycast(player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, player.UseDistance, ~LayerMaskTools.GetMaskWithAlives()))
			{
				if (hit.collider.TryGetComponent<IObject>(out var obj))
				{
					if (obj.CanPickup(player) || obj.CanUse(player))
					{
						if (obj is DroppedWearable droppedWearable)
							LookTargetLocalizer.Key = droppedWearable.Wearable.WearableData.Name;
						else
							LookTargetLocalizer.Key = obj.ObjectData.Name;
						
						LookTargetLocalizer.Apply();
					}
				}
			}
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not AI.Player)
				return;
			
			gameObject.SetActive(false);
			Hotbar.OnDeath();
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not AI.Player)
				return;

			gameObject.SetActive(true);
			Hotbar.OnSpawn();
		}
	}
}