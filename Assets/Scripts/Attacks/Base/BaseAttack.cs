using System.Runtime.CompilerServices;
using Attacks.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

namespace Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		[field: SerializeField]
		public Collider Trigger { get; private set; }

		[field: SerializeField]
		public float EnableTriggerAfter { get; private set; }
		[field: SerializeField]
		public float DisableTriggerAfter { get; private set; }

		[field: SerializeField]
		public bool Attach { get; private set; }

		private Transform target;
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public virtual void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Other);
				init = true;
			}
			
			Source = source;

			target = Attach ? attach : null;

			if (target == null)
			{
				thisTr.position = position + Vector3.up * 0.1f;
				thisTr.rotation = angles;
			}
			else
			{
				FollowTarget();
			}
			
			if (Trigger != null)
			{
				Trigger.enabled = false;
				trigger().Forget();
			}

			thisGo.SetActive(true);
			System.Play(true);
		}
		
		public void Update()
		{
			FollowTarget();
		}

		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), thisGo);
		}

		public virtual void OnTriggerEnabled()
		{
			Trigger.enabled = true;
		}

		public virtual void OnTriggerDisabled()
		{
			Trigger.enabled = false;
		}
		
		public void FollowTarget()
		{
			if (target == null)
				return;
			
			thisTr.position = target.position + Vector3.down * 0.95f;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid trigger()
		{
			await UniTask.WaitForSeconds(EnableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			OnTriggerEnabled();
			
			await UniTask.WaitForSeconds(DisableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			OnTriggerDisabled();
		}
	}
}