using System;
using System.Collections.Generic;
using AI.Interfaces;
using Attacks.Base;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Enums;
using UnityEngine;

namespace Attacks
{
	public class Pool : BaseAttack
	{
		[SerializeField]
		public Collider[] Colliders;

		[field: SerializeField]
		public EPoolType Type { get; private set; }
		
		[field: SerializeField]
		public virtual float Rate { get; private set; }

		[field: SerializeField]
		public virtual float Amount { get; private set; }
		
		[field: SerializeField]
		public virtual float Lifetime { get; set; }
		
		private List<IAlive> alives = new ();

		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);
			
			loop().Forget();
			
			if (Lifetime > 0f)
				lifetime().Forget();
		}

		public void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || alives.Contains(alive))
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
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			alives.Remove(alive);
		}

		public void OnPoolLooped(IAlive alive)
		{
			switch (Type)
			{
				case EPoolType.Health:
					alive.Heal(Amount, this, true);
					break;
				case EPoolType.Mana:
					alive.GenerateMana(Amount, this, true);
					break;
				default:
					throw new NotImplementedException();
			}
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