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
using Weapons.Interfaces;

namespace UI
{
	public class HUD : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text LookTarget;
		
		[SerializeField]
		public Image Crosshair;
		
		[SerializeField]
		public Image Cast;

		[SerializeField]
		public Image Cooldown;

		[SerializeField]
		public float LookTargetDistance = 2f;
		
		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}

		public void Update()
		{
			if (LookTarget.text != "")
				LookTarget.text = "";
			
			var player = AIManager.Instance.Player;
			if (player == null)
				return;

			var weapon = player.Weapon;
			if (weapon != null)
			{
				if (weapon.IsCasting)
				{
					var startingTime = weapon.LastStartedCast;
					var targetTime = startingTime + weapon.CastingTime;
					
					var amount = MathTools.Remap(Time.time, startingTime, targetTime, 0f, 1f);
					amount = Mathf.Clamp01(amount);
					
					Cast.fillAmount = amount;
				}
				else
				{
					Cast.fillAmount = 0f;
				}

				if (weapon.LastFinishedCast < 0)
				{
					Cooldown.fillAmount = 0f;
				}
				else
				{
					var finishTime = weapon.LastFinishedCast;
					var cooldownTime = finishTime + weapon.TimeBetweenAttacks;

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
			
			if (Physics.Raycast(player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), out var hit, LookTargetDistance, ~LayerMaskTools.GetMaskWithPlayer()))
			{
				var coll = hit.collider;
				
				if (coll.TryGetComponent<IPickupable>(out var pickupable) && pickupable.CanPickup(player))
				{
					LookTarget.text = pickupable.DisplayName;
				}
				else if (coll.TryGetComponent<IUsable>(out var usable) && usable.CanUse(player))
				{
					LookTarget.text = usable.DisplayName;
				}
			}
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			gameObject.SetActive(false);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player)
				return;

			gameObject.SetActive(true);
		}
	}
}