using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Objects
{
	public class Campfire : MonoBehaviour
	{
		[SerializeField]
		public float DamageRate = 0.15f;

		[SerializeField]
		public float Damage = 7;

		private readonly List<IAlive> alives = new ();

		public void Awake()
		{
			damage().Forget();
		}

		public void OnTriggerEnter(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null || alives.Contains(alive))
				return;
			
			alives.Add(alive);
		}

		public void OnTriggerExit(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null)
				return;

			alives.Remove(alive);
		}
		
		private async UniTask damage()
		{
			while (enabled)
			{
				await UniTask.WaitForSeconds(DamageRate);
			
				foreach (var alive in alives)
				{
					if (alive == null || !alive.IsAlive)
						continue;
				
					alive.Damage(Damage, this);
				}
			}
		}
	}
}