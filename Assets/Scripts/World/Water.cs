using System.Collections.Generic;
using AI.Interfaces;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Tools;
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
			while (true)
			{
				await UniTask.WaitForSeconds(DamageRate);
			
				if (this == null || !isActiveAndEnabled)
					return;
				
				foreach (var alive in alives)
				{
					if (alive.IsNull() || !alive.IsAlive)
						continue;
				
					alive.Damage(Damage, this, EElement.Unknown);
				}
			}
		}
	}
}