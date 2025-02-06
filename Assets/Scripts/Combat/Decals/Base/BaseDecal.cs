using System.Runtime.CompilerServices;
using Combat.Decals.Interfaces;
using Cysharp.Threading.Tasks;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Combat.Decals.Base
{
	public class BaseDecal : MonoBehaviour, IDecal
	{
		[field: SerializeField]
		public DecalData DecalData { get; private set; }
		
		[field: SerializeField]
		public DecalProjector Projector { get; private set; }
		
		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;
		private bool isFading;

		public void Spawn(Vector3 position, Quaternion angles, Transform attach)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				Projector.size = new Vector3(DecalData.Size, DecalData.Size, DecalData.Size);
				init = true;
			}
			
			thisTr.position = position;
			thisTr.rotation = angles;
			thisTr.parent = attach;
			
			thisTr.Rotate(new Vector3(0, 0, Random.Range(0, 360)), Space.Self);
			thisGo.SetActive(true);
			
			fade().Forget();
		}
		
		private async UniTask fade()
		{
			await UniTask.WaitForSeconds(DecalData.FadeAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;

			var duration = DecalData.FadeDuration;
			
			isFading = true;
			
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null || !isActiveAndEnabled)
					return;

				Projector.fadeFactor = 1 - normalizedTime;
				
				normalizedTime += Time.deltaTime / duration;
			}
			
			Destroy(gameObject);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
	}
}