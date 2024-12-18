using System;
using System.Runtime.CompilerServices;
using System.Threading;
using AI.Interfaces;
using Attacks.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Interfaces;
using Projectiles.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		public IWeapon Source { get; private set; }

		[field: SerializeField]
		public Rigidbody Rigidbody { get; private set; }
		[field: SerializeField]
		public Collider Collider { get; private set; }
		
		[field: SerializeField]
		public virtual float Distance { get; private set; }
		[field: SerializeField]
		public virtual float Damage { get; private set; }
		
		public virtual EAttackAngle AttackAngle { get; private set; }
		public virtual Type Attack { get; private set; }

		private CancellationTokenSource distanceToken;
		private Vector3 startingPosition;
		private Collider ignoreCollider;
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public void OnCollisionEnter(Collision collision)
		{
			if (Damage > 0)
			{
				var coll = collision.collider;
				if (coll != null)
				{
					if (coll.TryGetComponent<IAlive>(out var alive))
					{
						alive.Damage(Damage, this);
					}
					else if (coll.TryGetComponent<IBreakable>(out var breakable))
					{
						breakable.Damage(Damage, this);
					}
				}
			}
			
			if (Attack != null)
			{
				var contact = collision.contacts[0];
				
				Quaternion angles;

				switch (AttackAngle)
				{
					case EAttackAngle.Identity:
						angles = Quaternion.identity;
						break;
					case EAttackAngle.HitNormal:
						angles = Quaternion.FromToRotation(Vector3.up, contact.normal);
						break;
					case EAttackAngle.Owner:
						angles = Source.Owner.GetTransform().rotation;
						break;
					default:
						throw new NotImplementedException();
				}
				
				ObjectManager.Instance.CreateAttack(Attack, (Component)Source, contact.point, angles, contact.otherCollider.transform);
			}
				
			clearVelocityAndPool().Forget();
		}

		public void Update()
		{
			var distance = Vector3.Distance(startingPosition, thisTr.position);
			if (distance < Distance)
				return;

			clearVelocityAndPool().Forget();
		}
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Projectiles);
				init = true;
			}
			
			Source = source;

			startingPosition = origin;
			ignoreCollider = source.Owner.Body.Collider;
			
			Physics.IgnoreCollision(ignoreCollider, Collider, true);

			thisTr.position = origin;
			thisTr.eulerAngles = Vector3.zero;
			
			thisGo.SetActive(true);

			Rigidbody.AddForce(force, ForceMode.Impulse);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;

		private async UniTaskVoid clearVelocityAndPool()
		{
			if (distanceToken != null)
			{
				if (distanceToken.IsCancellationRequested)
					return;
				
				distanceToken.Cancel();
			}
			
			Physics.IgnoreCollision(ignoreCollider, Collider, false);
			
			thisGo.SetActive(false);

			await UniTask.NextFrame();

			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(GetType(), thisGo);
		}
	}
}