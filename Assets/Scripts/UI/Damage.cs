using AI;
using AI.Base;
using AI.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Damage : MonoBehaviour
	{
		[SerializeField]
		public Image Health;

		public void Awake()
		{
			BaseAlive.OnHealEvent.AddListener(OnHeal);
			BaseAlive.OnDamageEvent.AddListener(OnDamage);
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
			BaseAlive.OnSpawnEvent.AddListener(OnSpawn);
		}
		
		public void OnHeal(IAlive alive, int health, object source)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth);
		}
		
		public void OnDamage(IAlive alive, int damage, object source)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth);
		}
		
		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not Player)
				return;
			
			setHealth(0);
		}
		
		public void OnSpawn(IAlive alive)
		{
			if (alive is not Player player)
				return;

			setHealth(player.CurrentHealth);
		}
		
		private void setHealth(int health)
		{
			var color = Health.color;
			color.a = Mathf.SmoothStep(0f, 1f, 1f - (health / 100f));
			
			Health.color = color;
		}
	}
}