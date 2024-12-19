using System;
using System.Runtime.CompilerServices;
using System.Threading;
using AI.Interfaces;
using Attacks.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Interfaces;
using Projectiles.Interfaces;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public ProjectileData ProjectileData { get; private set; }

		public IWeapon Source { get; private set; }

		[field: SerializeField]
		public Rigidbody Rigidbody { get; private set; }
		[field: SerializeField]
		public Collider Collider { get; private set; }
		
		private CancellationTokenSource rangeToken;
		private Vector3 startingPosition;

		private Collider ignoreBodyCollider;
		private Collider ignoreFeetCollider;
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public void OnCollisionEnter(Collision collision)
		{
			Transform attach = null;
			
			if (ProjectileData.Damage > 0)
			{
				var coll = collision.collider;
				if (coll != null)
				{
					if (coll.TryGetComponent<IAlive>(out var alive))
					{
						attach = alive.GetTransform();
						alive.Damage(ProjectileData.Damage, this);
					}
					else if (coll.TryGetComponent<IBreakable>(out var breakable))
					{
						attach = breakable.GetTransform();
						breakable.Damage(ProjectileData.Damage, this);
					}
				}
			}
			
			if (ProjectileData.Attack != null)
			{
				var contact = collision.contacts[0];
				ObjectManager.Instance.CreateAttack(ProjectileData.Attack, (Component)Source, contact, attach);
			}
				
			clearVelocityAndPool().Forget();
		}

		public void Update()
		{
			var distance = Vector3.Distance(startingPosition, thisTr.position);
			if (distance < ProjectileData.Range)
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

			var body = source.Owner.Body;
			
			ignoreBodyCollider = body.BodyCollider;
			ignoreFeetCollider = body.FeetCollider;

			Physics.IgnoreCollision(ignoreBodyCollider, Collider, true);
			Physics.IgnoreCollision(ignoreFeetCollider, Collider, true);
			
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
			if (rangeToken != null)
			{
				if (rangeToken.IsCancellationRequested)
					return;
				
				rangeToken.Cancel();
			}
			
			Physics.IgnoreCollision(ignoreBodyCollider, Collider, false);
			Physics.IgnoreCollision(ignoreFeetCollider, Collider, false);
			
			thisGo.SetActive(false);

			await UniTask.NextFrame();

			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.AddToPool(ProjectileData, thisGo);
		}
	}
}