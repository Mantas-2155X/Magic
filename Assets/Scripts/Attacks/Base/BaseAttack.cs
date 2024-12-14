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

		private Transform target;
		
		public virtual void Spawn(Component source, Vector3 position, Quaternion angles)
		{
			Source = source;

			target = null;

			var tr = transform;
			tr.SetParent(World.World.Instance.Other);
			tr.position = position + Vector3.up * 0.1f;
			tr.rotation = angles;
			
			if (Trigger != null)
			{
				Trigger.enabled = false;
				trigger().Forget();
			}

			gameObject.SetActive(true);
			System.Play(true);
		}
		
		public virtual void Spawn(Component source, Transform attach)
		{
			Source = source;

			target = attach;

			var tr = transform;
			tr.SetParent(World.World.Instance.Other);

			FollowTarget();
			
			if (Trigger != null)
			{
				Trigger.enabled = false;
				trigger().Forget();
			}

			gameObject.SetActive(true);
			System.Play(true);
		}
		
		public void Update()
		{
			FollowTarget();
		}

		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
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
			
			transform.position = target.position + Vector3.down * 0.95f;
		}
		
		private async UniTask trigger()
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