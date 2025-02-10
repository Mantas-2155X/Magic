using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Events;
using Tools;
using UnityEngine;

namespace Objects.Base
{
	public class BaseButton : BaseObject
	{
		[SerializeField]
		public OnButtonUsedEvent OnButtonUsedEvent = new ();

		[SerializeField]
		public float IndicateDuration;
		
		private static readonly int emissionColor = Shader.PropertyToID("_EmissionColor");

		private CancellationTokenSource cancellationToken = new ();

		private Material sideMaterial;
		
		public override void Awake()
		{
			base.Awake();

			var rend = GetComponent<Renderer>();
			var mats = rend.materials;
			sideMaterial = mats[1];
		}
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();
			
			indicate(cancellationToken.Token).Forget();
			
			OnButtonUsedEvent?.Invoke();
			return true;
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnButtonUsedEvent, Color.blue);
		}
#endif

		private async UniTaskVoid indicate(CancellationToken token)
		{
			if (token.IsCancellationRequested)
				return;
			
			sideMaterial.color = Color.green;
			sideMaterial.SetColor(emissionColor, Color.green * 1.25f);
			sideMaterial.EnableKeyword("_EMISSION");
			
			await UniTask.WaitForSeconds(IndicateDuration, cancellationToken: token);
			
			if (cancellationToken.IsCancellationRequested)
				return;
			
			sideMaterial.color = Color.black;
			sideMaterial.DisableKeyword("_EMISSION");
		}
	}
}