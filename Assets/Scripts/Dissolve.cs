using Cysharp.Threading.Tasks;
using UnityEngine;

public class Dissolve : MonoBehaviour
{
	[SerializeField]
	public float DissolveAfter = 5f;
		
	[SerializeField]
	public float DissolveDuration = 1.5f;
	
	[SerializeField]
	public bool ShouldDissolve = true;
	
	private static readonly int dissolveAmount = Shader.PropertyToID("_DissolveAmount");

	public void Awake()
	{
		dissolve().Forget();
	}
	
	private async UniTask dissolve()
	{
		await UniTask.WaitForSeconds(ShouldDissolve ? DissolveAfter : DissolveAfter + DissolveDuration);
			
		if (this == null || !isActiveAndEnabled)
			return;

		if (ShouldDissolve)
		{
			var materials = GetComponent<Renderer>().materials;
			
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null || !isActiveAndEnabled)
					return;
				
				for (var i = 0; i < materials.Length; i++)
				{
					var material = materials[i];
					material.SetFloat(dissolveAmount, normalizedTime);
				}
				
				normalizedTime += Time.deltaTime / DissolveDuration;
			}
		}
		
		Destroy(gameObject);
	}
}