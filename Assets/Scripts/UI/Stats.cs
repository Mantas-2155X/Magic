using AI;
using AI.Base;
using AI.Interfaces;
using Combat.Enums;
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

		public void Awake()
		{
			BaseAlive.OnHealEvent.AddListener(OnHeal);
			BaseAlive.OnDamageEvent.AddListener(OnDamage);
			BaseAlive.OnManaGenerateEvent.AddListener(OnManaGenerate);
			BaseAlive.OnManaUseEvent.AddListener(OnManaUse);
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnHeal(IAlive alive, float health, object source)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth, player.MaximumHealth);
		}
		
		public void OnDamage(IAlive alive, float damage, object source, EElement type)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth, player.MaximumHealth);
		}
		
		public void OnManaGenerate(IAlive alive, float generated, object source)
		{
			if (alive is not Player player)
				return;

			setMana(player.CurrentMana, player.MaximumMana);
		}
		
		public void OnManaUse(IAlive alive, float used, object source)
		{
			if (alive is not Player player)
				return;

			setMana(player.CurrentMana, player.MaximumMana);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			setHealth(0, 100);
			setMana(0, 100);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth, player.MaximumHealth);
			setMana(player.CurrentMana, player.MaximumMana);
		}
		
		private void setHealth(float amount, float maximum)
		{
			if (amount > maximum)
				amount = maximum;

			var color = Red.color;
			color.a = Mathf.SmoothStep(0f, 1f, 1f - (amount / 100f));
			Red.color = color;
			
			var offset = HealthBottle.offsetMax;
			offset.y = -MathTools.Remap(amount, 0f, maximum, 111f, 0f);
			HealthBottle.offsetMax = offset;
		}
		
		private void setMana(float amount, float maximum)
		{
			if (amount > maximum)
				amount = maximum;
			
			var offset = ManaBottle.offsetMax;
			offset.y = -MathTools.Remap(amount, 0f, maximum, 111f, 0f);

			ManaBottle.offsetMax = offset;
		}
	}
}