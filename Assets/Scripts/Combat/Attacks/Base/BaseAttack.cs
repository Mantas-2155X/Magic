using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Enums;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using Tools;
using UnityEngine;

namespace Combat.Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		[field: SerializeField]
		public AttackData AttackData { get; private set; }
		
		public string ObjectID { get; set; }

		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		[field: SerializeField]
		public Collider[] Triggers { get; private set; }

		public Transform Target { get; set; }

		public readonly List<IAlive> TriggeredAlives = new ();
		public readonly List<IAlive> CurrentAlives = new ();
		
		public readonly List<IObject> TriggeredObjects = new ();
		public readonly List<IObject> CurrentObjects = new ();
		
		private GameObject thisGo;
		private Transform thisTr;

		private IAlive owner;
		
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

			if (AttackData.DropToGround && Physics.Raycast(position, Vector3.down, out var hit, float.MaxValue, ~LayerMaskTools.GetMaskWithAlives()))
				position = hit.point;
			
			Source = source;
			
			owner = null;
			owner = GetAlive();

			Target = AttackData.AttachToTarget ? attach : null;

			if (AttackData.FollowCaster && owner != null)
				Target = owner.GetTransform();
			
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
			if (PauseManager.IsPaused)
				return;
			
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
			if (AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
			{
				if (AttackData.IgnoreCaster && alive == GetAlive())
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
						alive.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
				
					alive.AddSlowSource(ObjectID, AttackData.Slow.Amount, AttackData.Slow.Duration);
					alive.AddParalyzeSource(ObjectID, AttackData.Paralyze.Duration);
				}
			
				CurrentAlives.Add(alive);
			}
			else if (other.TryGetComponent<IObject>(out var obj))
			{
				if (!TriggeredObjects.Contains(obj))
				{
					for (var i = 0; i < Triggers.Length; i++)
					{
						if (Triggers[i].bounds.Intersects(other.bounds))
							continue;

						return;
					}
				
					TriggeredObjects.Add(obj);
				
					if (AttackData.Damage != 0f)
						obj.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
				}
			
				CurrentObjects.Add(obj);
			}
		}
		
		public virtual void OnTriggerExit(Collider other)
		{
			if (AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				CurrentAlives.Remove(alive);
			else if (other.TryGetComponent<IObject>(out var obj))
				CurrentObjects.Remove(obj);
		}

		public virtual void OnTriggersEnabled()
		{
			TriggeredAlives.Clear();
			CurrentAlives.Clear();
			
			TriggeredObjects.Clear();
			CurrentObjects.Clear();

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
			
			var scale = Target.localScale.y;
			thisTr.position = Target.position + -Target.up * (0.95f * scale) + (AttackData.AttachOffset * scale);
		}
		
		public IAlive GetAlive()
		{
			if (Source == null)
				return null;

			if (owner != null)
				return owner;
			
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
			
			if (this == null || !isActiveAndEnabled)
				return;

			OnTriggersEnabled();
			
			await UniTask.WaitForSeconds(AttackData.DisableTriggerAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;

			OnTriggersDisabled();
		}
	}
}