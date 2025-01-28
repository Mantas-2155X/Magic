using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class HealthGib : BaseObject
	{
		[SerializeField]
		public float HealAmount;

		[SerializeField]
		public float DissolveAfter;
		
		[SerializeField]
		public float DissolveDuration = 1.5f;

		private static readonly int dissolveAmount = Shader.PropertyToID("_DissolveAmount");

		private bool isDissolving;
		
		public override void OnEnable()
		{
			base.OnEnable();
			
			if (DissolveAfter == 0f)
				return;
			
			dissolve().Forget();
		}
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			user.RestoreHealth(HealAmount, this);
			return true;
		}

		public override bool CanUse(IAlive user)
		{
			return base.CanUse(user) && !isDissolving;
		}
		
		private async UniTask dissolve()
		{
			await UniTask.WaitForSeconds(DissolveAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;

			isDissolving = true;

			var material = GetComponent<Renderer>().material;
			
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null || !isActiveAndEnabled)
					return;
				
				material.SetFloat(dissolveAmount, normalizedTime);
				
				normalizedTime += Time.deltaTime / DissolveDuration;
			}
			
			Destroy(gameObject);
		}
	}
}