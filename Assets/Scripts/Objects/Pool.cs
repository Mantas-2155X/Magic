using System.Collections.Generic;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Enums;
using UnityEngine;

namespace Objects
{
	public class Pool : MonoBehaviour
	{
		[SerializeField]
		public Collider[] Colliders;

		[SerializeField]
		public EPoolType Type = EPoolType.Health;
		
		[SerializeField]
		public float Rate = 0.5f;

		[SerializeField]
		public float Amount = 2f;
		
		private List<IAlive> alives = new ();

		public void Awake()
		{
			loop().Forget();
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

		private async UniTask loop()
		{
			while (enabled)
			{
				await UniTask.WaitForSeconds(Rate);

				foreach (var alive in alives)
				{
					if (alive == null || !alive.IsAlive)
						continue;

					switch (Type)
					{
						case EPoolType.Health:
							alive.Heal(Amount, this, true);
							break;
						case EPoolType.Mana:
							alive.GenerateMana(Amount, this, true);
							break;
					}
				}
			}
		}
	}
}