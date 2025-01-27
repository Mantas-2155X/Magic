using System.Runtime.CompilerServices;
using System.Threading;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Enums;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public ProjectileData ProjectileData { get; private set; }

		public Component Source { get; private set; }

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

		private IAlive owner;

		private AttackData attackData;
		
		private float spellRange;
		
		private bool init;
		
		public void OnCollisionEnter(Collision collision)
		{
			Transform attach = null;
			
			if (ProjectileData.Damage > 0)
			{
				var coll = collision.collider;
				
				if (AIManager.Instance.AlivesColliderMap.TryGetValue(coll, out var alive))
				{
					attach = alive.GetTransform();
					alive.Damage(ProjectileData.Damage, GetAlive(), ProjectileData.Element);
					alive.AddSlowSource(ProjectileData.Slow.Amount, ProjectileData.Slow.Duration);
				}
				else if (coll.TryGetComponent<IObject>(out var obj))
				{
					attach = obj.GetTransform();
					obj.Damage(ProjectileData.Damage, GetAlive(), ProjectileData.Element);
				}
			}
			
			if (attackData != null)
			{
				var contact = collision.contacts[0];
				ObjectManager.Instance.CreateAttack(attackData, Source, contact, attach);
			}
				
			clearVelocityAndPool().Forget();
		}

		public void Update()
		{
			var distance = Vector3.Distance(startingPosition, thisTr.position);
			if (distance < spellRange)
				return;

			clearVelocityAndPool().Forget();
		}
		
		public void Spawn(Component source, float range, AttackData attack, Vector3 origin, Vector3 force)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Projectiles);
				init = true;
			}
			
			Source = source;
			
			owner = null;
			owner = GetAlive();

			startingPosition = origin;
			spellRange = range;
			attackData = attack;

			if (owner != null)
			{
				var body = owner.Body;
			
				ignoreBodyCollider = body.BodyCollider;
				ignoreFeetCollider = body.FeetCollider;

				Physics.IgnoreCollision(ignoreBodyCollider, Collider, true);
				Physics.IgnoreCollision(ignoreFeetCollider, Collider, true);
			}
			else
			{
				ignoreBodyCollider = null;
				ignoreFeetCollider = null;
			}
			
			thisTr.position = origin;
			thisTr.eulerAngles = Vector3.zero;
			
			thisGo.SetActive(true);

			Rigidbody.AddForce(force, ForceMode.Impulse);
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

		private async UniTaskVoid clearVelocityAndPool()
		{
			if (rangeToken != null)
			{
				if (rangeToken.IsCancellationRequested)
					return;
				
				rangeToken.Cancel();
			}
			
			if (ignoreBodyCollider != null && ignoreFeetCollider != null)
			{
				Physics.IgnoreCollision(ignoreBodyCollider, Collider, false);
				Physics.IgnoreCollision(ignoreFeetCollider, Collider, false);
			}
			
			thisGo.SetActive(false);

			await UniTask.NextFrame();

			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			PoolingManager.Instance.Add(ProjectileData, thisGo);
		}
	}
}