using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Enums;
using UnityEngine;

namespace Objects.Base
{
	public class BasePool : MonoBehaviour
	{
		[SerializeField]
		public ParticleSystem System;
		
		[SerializeField]
		public Collider[] Colliders;

		[field: SerializeField]
		public virtual EPoolType Type { get; private set; }
		
		[field: SerializeField]
		public virtual float Rate { get; private set; }

		[field: SerializeField]
		public virtual float Amount { get; private set; }
		
		[field: SerializeField]
		public virtual float Lifetime { get; set; }
		
		private List<IAlive> alives = new ();

		public void OnEnable()
		{
			System.Play(true);
			
			loop().Forget();
			
			if (Lifetime > 0f)
				lifetime().Forget();
		}

		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}

		public void OnTriggerEnter(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null || alives.Contains(alive))
				return;

			for (var i = 0; i < Colliders.Length; i++)
			{
				var coll = Colliders[i];
				if (!coll.bounds.Intersects(other.bounds))
					return;
			}

			alives.Add(alive);
		}

		public void OnTriggerExit(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null)
				return;

			alives.Remove(alive);
		}

		public virtual void OnPoolLooped(IAlive alive)
		{
			
		}
		
		private async UniTaskVoid loop()
		{
			while (enabled)
			{
				await UniTask.WaitForSeconds(Rate);

				foreach (var alive in alives)
				{
					if (alive == null || !alive.IsAlive)
						continue;

					OnPoolLooped(alive);
				}
			}
		}

		private async UniTaskVoid lifetime()
		{
			await UniTask.WaitForSeconds(Lifetime);
			
			System.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}
	}
}