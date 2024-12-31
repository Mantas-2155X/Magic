using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Enums;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		[field: SerializeField]
		public AttackData AttackData { get; private set; }
		
		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		[field: SerializeField]
		public Collider[] Triggers { get; private set; }

		public Transform Target { get; private set; }

		public readonly List<IAlive> TriggeredAlives = new ();
		public readonly List<IAlive> CurrentAlives = new ();
		
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

			Target = AttackData.AttachToTarget ? attach : null;

			if (Target == null)
			{
				thisTr.position = position + Vector3.up * 0.1f;
				thisTr.rotation = angles;
			}
			else
			{
				FollowTarget();
			}
			
			if (Triggers != null)
			{
				for (var i = 0; i < Triggers.Length; i++)
					Triggers[i].enabled = false;
				
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

		public virtual void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;
			
			if (!TriggeredAlives.Contains(alive))
			{
				for (var i = 0; i < Triggers.Length; i++)
				{
					if (Triggers[i].bounds.Intersects(other.bounds))
						continue;

					return;
				}
				
				TriggeredAlives.Add(alive);
				
				if (AttackData.Damage != 0f)
					alive.Damage(AttackData.Damage, this, EDamageType.Magic);
			}
			
			CurrentAlives.Add(alive);
		}
		
		public virtual void OnTriggerExit(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			CurrentAlives.Remove(alive);
		}

		public virtual void OnTriggersEnabled()
		{
			TriggeredAlives.Clear();
			CurrentAlives.Clear();

			for (var i = 0; i < Triggers.Length; i++)
				Triggers[i].enabled = true;
		}

		public virtual void OnTriggersDisabled()
		{
			for (var i = 0; i < Triggers.Length; i++)
				Triggers[i].enabled = false;
		}
		
		public void FollowTarget()
		{
			if (Target == null)
				return;
			
			thisTr.position = Target.position + Vector3.down * 0.95f + AttackData.AttachOffset;
		}
		
		public IAlive GetAlive()
		{
			if (Source == null)
				return null;

			switch (Source)
			{
				case IAlive alive:
					return alive;
				case ISpell spell:
					return spell.Owner;
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

			OnTriggersEnabled();
			
			await UniTask.WaitForSeconds(AttackData.DisableTriggerAfter);
			
			if (!isActiveAndEnabled)
				return;

			OnTriggersDisabled();
		}
	}
}