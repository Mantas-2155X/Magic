using System.Collections.Generic;
using AI.Interfaces;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

namespace World
{
	public class Water : MonoBehaviour
	{
		[SerializeField]
		public float DamageRate = 0.1f;

		[SerializeField]
		public float Damage = 12;

		private List<IAlive> alives = new ();
		
		public void OnEnable()
		{
			damage().Forget();
		}

		public void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || alives.Contains(alive))
				return;
			
			alives.Add(alive);
		}

		public void OnTriggerExit(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			alives.Remove(alive);
		}

		private async UniTaskVoid damage()
		{
			while (enabled)
			{
				await UniTask.WaitForSeconds(DamageRate);
			
				foreach (var alive in alives)
				{
					if (alive == null || !alive.IsAlive)
						continue;
				
					alive.Damage(Damage, this, EElement.Unknown);
				}
			}
		}
	}
}