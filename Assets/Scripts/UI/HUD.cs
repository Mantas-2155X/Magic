using System;
using System.Collections.Generic;
using System.Linq;
using AI.Base;
using AI.Interfaces;
using Combat.Enums;
using Managers;
using Objects;
using Objects.Interfaces;
using State.Interfaces;
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
		public Image Health;
		
		[SerializeField]
		public Image Mana;
		
		[SerializeField]
		public Image[] Energy;
		
		[SerializeField]
		public float Smoothing = 5f;

		[SerializeField]
		public Image Red;

		[SerializeField]
		public AnimationCurve HealthRedCurve;

		[SerializeField]
		public Transform DamageSourceTemplate;

		[SerializeField]
		public Spellbook.Spellbook Spellbook;

		[SerializeField]
		public Hotbar.Hotbar Hotbar;
		
		private readonly Dictionary<Transform, Tuple<Vector3, float>> damageSources = new ();
		private readonly List<Transform> clearDamageSources = new ();

		private float targetHealth;
		private float targetMana;
		private float targetEnergy;
		
		private float targetRed;

		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnRestoreHealthEvent.AddListener(OnRestoreHealth);
			BaseAlive.OnDamageEvent.AddListener(OnDamage);
			BaseAlive.OnRestoreManaEvent.AddListener(OnRestoreMana);
			BaseAlive.OnRestoreEnergyEvent.AddListener(OnRestoreEnergy);
			BaseAlive.OnTakeManaEvent.AddListener(OnTakeMana);
			BaseAlive.OnTakeEnergyEvent.AddListener(OnTakeEnergy);
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnDestroy()
		{
			BaseAlive.OnRestoreHealthEvent.RemoveListener(OnRestoreHealth);
			BaseAlive.OnDamageEvent.RemoveListener(OnDamage);
			BaseAlive.OnRestoreManaEvent.RemoveListener(OnRestoreMana);
			BaseAlive.OnRestoreEnergyEvent.RemoveListener(OnRestoreEnergy);
			BaseAlive.OnTakeManaEvent.RemoveListener(OnTakeMana);
			BaseAlive.OnTakeEnergyEvent.RemoveListener(OnTakeEnergy);
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
			if (spell.NotNull())
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
			
			// Damage sources
			var time = Time.time;
			var playerTr = player.GetTransform();
				
			var playerPos = playerTr.position;
			playerPos.y = 0f;
				
			clearDamageSources.Clear();
				
			foreach (var pair in damageSources)
			{
				var tr = pair.Key;
				var go = tr.gameObject;
					
				if (pair.Value == null)
				{
					if (go.activeSelf)
						go.SetActive(false);
						
					continue;
				}

				if (time > pair.Value.Item2)
				{
					clearDamageSources.Add(pair.Key);
					go.SetActive(false);
					continue;
				}
					
				if (!go.activeSelf)
					go.SetActive(true);

				var direction = (playerPos - pair.Value.Item1).normalized;
					
				var newDirection = new Vector3(-direction.x, 0f, -direction.z);
				var projected = Vector3.ProjectOnPlane(playerTr.forward, Vector3.up);
					
				var angle = Vector3.SignedAngle(projected, newDirection, Vector3.up);
				var radian = angle * Mathf.Deg2Rad;
					
				tr.localPosition = new Vector3(Mathf.Sin(radian), Mathf.Cos(radian), 0f) * 13f;
			}

			for (var i = clearDamageSources.Count - 1; i >= 0; i--)
				damageSources[clearDamageSources[i]] = null;
			
			// Health
			{
				var color = Red.color;
				color.a = Mathf.Lerp(color.a, targetRed, Time.unscaledDeltaTime * Smoothing);
				Red.color = color;
				
				var amount = Health.fillAmount;
				amount = Mathf.Lerp(amount, targetHealth, Time.unscaledDeltaTime * Smoothing);
				Health.fillAmount = amount;
			}
			
			// Mana
			{
				var amount = Mana.fillAmount;
				amount = Mathf.Lerp(amount, targetMana, Time.unscaledDeltaTime * Smoothing);
				Mana.fillAmount = amount;
			}
			
			// Energy
			for (var i = 0; i < Energy.Length; i++)
			{
				var energy = Energy[i];
				
				var amount = energy.fillAmount;
				amount = Mathf.Lerp(amount, targetEnergy, Time.unscaledDeltaTime * Smoothing);
				energy.fillAmount = amount;
			}
		}
		
		public void OnRestoreHealth(IAlive alive, float health, object source)
		{
			if (alive is not AI.Player player)
				return;

			setHealth(player.CurrentHealth, player.Data.Health);
		}
		
		public void OnDamage(IAlive alive, float damage, object source, EElement type)
		{
			if (alive is not AI.Player player)
				return;

			setHealth(player.CurrentHealth, player.Data.Health);
			
			if (type == EElement.Unknown || source is not IIdentifiable identifiable || identifiable.IsNull())
				return;

			Transform damageIndicator = null;
			
			foreach (var pair in damageSources)
			{
				if (pair.Value != null)
					continue;

				damageIndicator = pair.Key;
				break;
			}

			if (damageIndicator == null)
				damageIndicator = Instantiate(DamageSourceTemplate.gameObject, DamageSourceTemplate.parent).transform;

			var pos = identifiable.GetTransform().position;
			pos.y = 0f;
			
			damageSources[damageIndicator] = new Tuple<Vector3, float>(pos, Time.time + 2.5f);
		}
		
		public void OnRestoreMana(IAlive alive, float generated, object source)
		{
			if (alive is not AI.Player player)
				return;

			setMana(player.CurrentMana, player.Data.Mana);
		}
		
		public void OnRestoreEnergy(IAlive alive, float generated, object source)
		{
			if (alive is not AI.Player player)
				return;

			setEnergy(player.CurrentEnergy, player.Data.Energy);
		}
		
		public void OnTakeMana(IAlive alive, float used, object source)
		{
			if (alive is not AI.Player player)
				return;

			setMana(player.CurrentMana, player.Data.Mana);
		}
		
		public void OnTakeEnergy(IAlive alive, float used, object source)
		{
			if (alive is not AI.Player player)
				return;

			setEnergy(player.CurrentEnergy, player.Data.Energy);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not AI.Player)
				return;
			
			gameObject.SetActive(false);
			Hotbar.OnDeath();
			
			resetDamageSources();
			
			setHealth(0, 100);
			setMana(0, 100);
			setEnergy(0, 100);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not AI.Player player)
				return;

			gameObject.SetActive(true);
			Hotbar.OnSpawn();
			
			resetDamageSources();
			
			setHealth(player.CurrentHealth, player.Data.Health);
			setMana(player.CurrentMana, player.Data.Mana);
			setEnergy(player.CurrentEnergy, player.Data.Energy);
		}

		private void resetDamageSources()
		{
			var keys = damageSources.Keys.ToList();
			for (var i = keys.Count - 1; i >= 0; i--)
				damageSources[keys[i]] = null;
		}
		
		private void setHealth(float amount, float maximum)
		{
			if (amount > maximum)
				amount = maximum;

			targetRed = HealthRedCurve.Evaluate(1 - amount / maximum);
			targetHealth = MathTools.Remap(amount, 0f, maximum, 0f, 0.3f);
		}
		
		private void setMana(float amount, float maximum)
		{
			if (amount > maximum)
				amount = maximum;
			
			targetMana = MathTools.Remap(amount, 0f, maximum, 0f, 0.3f);
		}
		
		private void setEnergy(float amount, float maximum)
		{
			if (amount > maximum)
				amount = maximum;
			
			targetEnergy = MathTools.Remap(amount, 0f, maximum, 0f, 0.5f);
		}
	}
}