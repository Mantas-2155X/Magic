using System.Threading;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
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

		private Material[] instancedMaterials;
		
		#region Identify / SaveLoad

		public override bool ShouldSave => false;

		#endregion
		
		public override void Awake()
		{
			base.Awake();

			instancedMaterials = GetComponent<Renderer>().materials;
		}
		
		public override void OnDestroy()
		{
			base.OnDestroy();
			
			if (instancedMaterials == null)
				return;

			for (var i = 0; i < instancedMaterials.Length; i++)
			{
				Destroy(instancedMaterials[i]);
				instancedMaterials[i] = null;
			}
		}
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			if (ObjectData.UseAudio != null)
				AudioManager.Instance.PlayAtPoint(ObjectData.UseAudio, GetTransform().position);

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
			
			var sideMaterial = instancedMaterials[1];
			
			sideMaterial.color = Color.green;
			sideMaterial.SetColor(emissionColor, Color.green * 1.25f);
			sideMaterial.EnableKeyword("_EMISSION");
			
			await UniTask.WaitForSeconds(IndicateDuration, cancellationToken: token);
			
			if (this == null || !isActiveAndEnabled || cancellationToken.IsCancellationRequested)
				return;
			
			sideMaterial.color = Color.black;
			sideMaterial.DisableKeyword("_EMISSION");
		}
	}
}