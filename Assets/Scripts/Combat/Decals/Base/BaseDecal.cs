using System.Runtime.CompilerServices;
using Combat.Decals.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Combat.Decals.Base
{
	public class BaseDecal : MonoBehaviour, IDecal
	{
		[field: SerializeField]
		public DecalData DecalData { get; private set; }
		
		public string ObjectID { get; set; }

		[field: SerializeField]
		public DecalProjector Projector { get; private set; }
		
		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		public void Spawn(Vector3 position, Quaternion angles, Transform attach)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Decals);
				init = true;
			}

			Projector.size = new Vector3(DecalData.Size, DecalData.Size, DecalData.Size / 2f);
			Projector.fadeFactor = 1f;
			
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
			
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null || !isActiveAndEnabled)
					return;

				Projector.fadeFactor = 1 - normalizedTime;
				
				normalizedTime += Time.deltaTime / duration;
			}
			
			thisTr.SetParent(World.World.Instance.Decals);
			
			ObjectID = "";
			PoolingManager.Instance.Add(DecalData, thisGo);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
	}
}