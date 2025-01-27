using System;
using AI;
using AI.Base;
using AI.Interfaces;
using Combat.Enums;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Stats : MonoBehaviour
	{
		[SerializeField]
		public Image Red;

		[SerializeField]
		public RectTransform HealthBottle;

		[SerializeField]
		public RectTransform ManaBottle;

		[SerializeField]
		public AnimationCurve HealthRedCurve;

		[SerializeField]
		public TMP_Text HealthText;
		
		[SerializeField]
		public TMP_Text ManaText;
		
		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnRestoreHealthEvent.AddListener(OnRestoreHealth);
			BaseAlive.OnDamageEvent.AddListener(OnDamage);
			BaseAlive.OnRestoreManaEvent.AddListener(OnRestoreMana);
			BaseAlive.OnTakeManaEvent.AddListener(OnTakeMana);
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}

		public void OnDestroy()
		{
			BaseAlive.OnRestoreHealthEvent.RemoveListener(OnRestoreHealth);
			BaseAlive.OnDamageEvent.RemoveListener(OnDamage);
			BaseAlive.OnRestoreManaEvent.RemoveListener(OnRestoreMana);
			BaseAlive.OnTakeManaEvent.RemoveListener(OnTakeMana);
			BaseAlive.OnDeathEvent.RemoveListener(OnDeath);
			BaseAlive.OnSpawnEvent.RemoveListener(OnSpawn);
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
		}
		
		public void OnRestoreMana(IAlive alive, float generated, object source)
		{
			if (alive is not AI.Player player)
				return;

			setMana(player.CurrentMana, player.Data.Mana);
		}
		
		public void OnTakeMana(IAlive alive, float used, object source)
		{
			if (alive is not AI.Player player)
				return;

			setMana(player.CurrentMana, player.Data.Mana);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not AI.Player)
				return;
			
			setHealth(0, 100);
			setMana(0, 100);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not AI.Player player)
				return;

			gameObject.SetActive(true);
			setHealth(player.CurrentHealth, player.Data.Health);
			setMana(player.CurrentMana, player.Data.Mana);
		}
		
		private void setHealth(float amount, float maximum)
		{
			HealthText.text = amount.ToString("0.#");

			if (amount > maximum)
				amount = maximum;

			var color = Red.color;
			color.a = HealthRedCurve.Evaluate(1 - (amount / maximum));
			Red.color = color;
			
			var offset = HealthBottle.offsetMax;
			offset.y = -MathTools.Remap(amount, 0f, maximum, 111f, 0f);
			HealthBottle.offsetMax = offset;
		}
		
		private void setMana(float amount, float maximum)
		{
			ManaText.text = amount.ToString("0.#");

			if (amount > maximum)
				amount = maximum;
			
			var offset = ManaBottle.offsetMax;
			offset.y = -MathTools.Remap(amount, 0f, maximum, 111f, 0f);

			ManaBottle.offsetMax = offset;
		}
	}
}