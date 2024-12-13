using System;
using AI.Interfaces;
using Attacks.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

namespace Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		
		[field: SerializeField]
		public Collider Trigger { get; private set; }

		[field: SerializeField]
		public float EnableTriggerAfter { get; private set; }
		[field: SerializeField]
		public float DisableTriggerAfter { get; private set; }

		public IAlive Owner { get; private set; }
		
		public virtual Type Type { get; private set; }
		
		public virtual void Spawn(IAlive owner, Vector3 position, bool parent)
		{
			Owner = owner;

			var tr = transform;
			
			if (parent)
				tr.SetParent(World.World.Instance.Other);

			tr.position = position + Vector3.up * 0.1f;
			tr.eulerAngles = Vector3.zero;

			Trigger.enabled = false;

			gameObject.SetActive(true);
			System.Play(true);
			
			if (Trigger != null)
				trigger().Forget();
		}
		
		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		private async UniTask trigger()
		{
			await UniTask.WaitForSeconds(EnableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			Trigger.enabled = true;
			
			await UniTask.WaitForSeconds(DisableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			Trigger.enabled = false;
		}
	}
}