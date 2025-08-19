using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components
{
	public class Dissolve : MonoBehaviour
	{
		[SerializeField]
		public float DissolveAfter = 5f;
		
		[SerializeField]
		public float DissolveDuration = 1.5f;
	
		[SerializeField]
		public bool ShouldDissolve = true;
	
		private static readonly int dissolveAmount = Shader.PropertyToID("_DissolveAmount");

		private Material[] instancedMaterials;
		
		public void Awake()
		{
			dissolve().Forget();
		}

		public void OnDestroy()
		{
			if (instancedMaterials == null)
				return;

			for (var i = 0; i < instancedMaterials.Length; i++)
			{
				Destroy(instancedMaterials[i]);
				instancedMaterials[i] = null;
			}
		}

		private async UniTask dissolve()
		{
			await UniTask.WaitForSeconds(ShouldDissolve ? DissolveAfter : DissolveAfter + DissolveDuration);
			
			if (this == null || !isActiveAndEnabled)
				return;

			if (ShouldDissolve)
			{
				instancedMaterials = GetComponent<Renderer>().materials;
			
				var normalizedTime = 0.0f;
				while (normalizedTime < 1.0f)
				{
					await UniTask.NextFrame();

					if (this == null || !isActiveAndEnabled)
						return;
				
					for (var i = 0; i < instancedMaterials.Length; i++)
					{
						var material = instancedMaterials[i];
						material.SetFloat(dissolveAmount, normalizedTime);
					}
				
					normalizedTime += Time.deltaTime / DissolveDuration;
				}
			}
		
			Destroy(gameObject);
		}
	}
}