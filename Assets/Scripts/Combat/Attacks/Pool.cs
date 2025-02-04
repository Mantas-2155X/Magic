using System;
using AI.Interfaces;
using Combat.Attacks.Base;
using Combat.Attacks.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat.Attacks
{
	public class Pool : BaseAttack
	{
		[field: SerializeField]
		public EPoolType Type { get; private set; }
		
		[field: SerializeField]
		public virtual float Rate { get; private set; }

		[field: SerializeField]
		public virtual float Amount { get; private set; }
		
		[field: SerializeField]
		public virtual float Lifetime { get; set; }
		
		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);
			
			loop().Forget();
			
			if (Lifetime > 0f)
				lifetime().Forget();
		}

		public void OnPoolLooped(IAlive alive)
		{
			switch (Type)
			{
				case EPoolType.Health:
					alive.RestoreHealth(Amount, this);
					break;
				case EPoolType.Mana:
					alive.RestoreMana(Amount, this);
					break;
				case EPoolType.Energy:
					alive.RestoreEnergy(Amount, this);
					break;
				default:
					throw new NotImplementedException();
			}
		}
		
		private async UniTaskVoid loop()
		{
			while (true)
			{
				await UniTask.WaitForSeconds(Rate);

				if (this == null || !isActiveAndEnabled)
					return;
				
				for (var i = 0; i < CurrentAlives.Count; i++)
				{
					var alive = CurrentAlives[i];
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