using AI;
using AI.Base;
using AI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Stats : MonoBehaviour
	{
		[SerializeField]
		public Image Health;

		[SerializeField]
		public Image Mana;

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

			setHealth(player.CurrentHealth);
		}
		
		public void OnDamage(IAlive alive, float damage, object source)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth);
		}
		
		public void OnManaGenerate(IAlive alive, float generated, object source)
		{
			if (alive is not Player player)
				return;

			setMana(player.CurrentMana);
		}
		
		public void OnManaUse(IAlive alive, float used, object source)
		{
			if (alive is not Player player)
				return;

			setMana(player.CurrentMana);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			setHealth(0);
			setMana(0);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth);
			setMana(player.CurrentMana);
		}
		
		private void setHealth(float health)
		{
			var color = Health.color;
			color.a = Mathf.SmoothStep(0f, 1f, 1f - (health / 100f));
			
			Health.color = color;
		}
		
		private void setMana(float mana)
		{
			Mana.fillAmount = Mathf.SmoothStep(0f, 1f, (mana / 100f));
		}
	}
}