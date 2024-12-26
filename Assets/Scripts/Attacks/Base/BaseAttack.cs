using System.Runtime.CompilerServices;
using AI.Interfaces;
using Attacks.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Projectiles.Interfaces;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		[field: SerializeField]
		public AttackData AttackData { get; private set; }
		
		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		[field: SerializeField]
		public Collider Trigger { get; private set; }

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
				thisTr.SetParent(World.World.Instance.Attacks);
				init = true;
			}
			
			Source = source;

			target = AttackData.AttachToTarget ? attach : null;

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
			
			if (System != null)
				System.Play(true);
		}
		
		public void Update()
		{
			FollowTarget();
		}

		public virtual void OnDisable()
		{
			PoolingManager.Instance.Add(AttackData, thisGo);
		}

		public void OnParticleSystemStopped()
		{
			PoolingManager.Instance.Add(AttackData, thisGo);
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
		
		public IAlive GetAlive()
		{
			if (Source == null)
				return null;

			switch (Source)
			{
				case IAlive alive:
					return alive;
				case IWeapon weapon:
					return weapon.GetAlive();
				case IAttack attack:
					return attack.GetAlive();
				case IProjectile projectile:
					return projectile.GetAlive();
				default:
					return null;
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid trigger()
		{
			await UniTask.WaitForSeconds(AttackData.EnableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			OnTriggerEnabled();
			
			await UniTask.WaitForSeconds(AttackData.DisableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			OnTriggerDisabled();
		}
	}
}