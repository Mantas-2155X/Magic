using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public ProjectileData ProjectileData { get; private set; }

		public bool ShouldSave => true;

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public IIdentifiable Source { get; private set; }

		[field: SerializeField]
		public Rigidbody Rigidbody { get; private set; }
		[field: SerializeField]
		public Collider Collider { get; private set; }
		
		public Vector3 StartingPosition { get; private set; }
		public AttackData AttackData { get; private set; }
		public float SpellRange { get; private set; }

		public float CreatedTime { get; private set; }

		private CancellationTokenSource rangeToken;

		private Collider ignoreBodyCollider;
		private Collider ignoreFeetCollider;
		
		private GameObject thisGo;
		private Transform thisTr;

		private IAlive owner;
		
		private bool init;
		
		#region Identify / SaveLoad

		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var transformState = TransformState.Read(thisTr);
			if (transformState != null)
				dict[typeof(Transform).ToString()] = JObject.FromObject(transformState);

			var rigidbodyState = RigidbodyState.Read(Rigidbody);
			if (rigidbodyState != null)
				dict[typeof(Rigidbody).ToString()] = JObject.FromObject(rigidbodyState);

			var baseProjectileState = BaseProjectileState.Read(this);
			if (baseProjectileState != null)
				dict[typeof(BaseProjectile).ToString()] = JObject.FromObject(baseProjectileState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState))
				TransformState.Apply(thisTr, transformState.ToObject<TransformState>());
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState))
				RigidbodyState.Apply(Rigidbody, rigidbodyState.ToObject<RigidbodyState>());
			
			if (data.TryGetValue(typeof(BaseProjectile).ToString(), out var baseProjectileState))
				BaseProjectileState.Apply(this, baseProjectileState.ToObject<BaseProjectileState>());
		}

		public virtual void SetState(Vector3 startingPosition)
		{
			StartingPosition = startingPosition;
		}
		
		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		public void OnCollisionEnter(Collision collision)
		{
			IIdentifiable attach = null;
			
			var contact = collision.contacts[0];
			
			if (ProjectileData.Damage > 0)
			{
				var coll = collision.collider;
				
				if (AIManager.Instance.AlivesColliderMap.TryGetValue(coll, out var alive))
				{
					attach = alive;
					alive.Damage(ProjectileData.Damage, GetAlive(), ProjectileData.Element);
					alive.AddSlowSource(ObjectID, ProjectileData.Slow.Amount, ProjectileData.Slow.Duration);
					alive.AddParalyzeSource(ObjectID, ProjectileData.Paralyze.Duration);
				}
				else if (coll.TryGetComponent<IObject>(out var obj))
				{
					attach = obj;
					obj.Damage(ProjectileData.Damage, GetAlive(), ProjectileData.Element);
				}
			}
			
			if (AttackData != null)
				ObjectManager.Instance.CreateAttack(AttackData, Source, contact, attach);

			if (ProjectileData.Decal != null)
				ObjectManager.Instance.CreateDecal(ProjectileData.Decal, contact, attach);
				
			clearVelocityAndPool().Forget();
		}

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			var distance = Vector3.Distance(StartingPosition, thisTr.position);
			if (distance < SpellRange)
				return;

			clearVelocityAndPool().Forget();
		}
		
		public void Spawn(IIdentifiable source, float range, AttackData attack, Vector3 origin, Vector3 force, float elapsedTime = 0f)
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

			StartingPosition = origin;
			SpellRange = range;
			AttackData = attack;

			CreatedTime = Time.time;

			if (owner.NotNull())
			{
				var body = owner.Body;
			
				ignoreBodyCollider = body.BodyCollider;
				Physics.IgnoreCollision(ignoreBodyCollider, Collider, true);

				if (body.FeetCollider != null)
				{
					ignoreFeetCollider = body.FeetCollider;
					Physics.IgnoreCollision(ignoreFeetCollider, Collider, true);
				}
				else
				{
					ignoreFeetCollider = null;
				}
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

			var particleSystems = thisGo.GetComponentsInChildren<ParticleSystem>();
			for (var i = 0; i < particleSystems.Length; i++)
			{
				var system = particleSystems[i];
				system.Simulate(elapsedTime, false);
				system.Play();
			}
		}
		
		public IAlive GetAlive()
		{
			if (Source.IsNull())
				return null;

			if (owner.NotNull())
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
			
			if (ignoreBodyCollider != null)
				Physics.IgnoreCollision(ignoreBodyCollider, Collider, false);
			
			if (ignoreFeetCollider != null)
				Physics.IgnoreCollision(ignoreFeetCollider, Collider, false);
			
			thisGo.SetActive(false);

			await UniTask.NextFrame();

			if (this == null)
				return;
			
			Rigidbody.linearVelocity = Vector3.zero;
			Rigidbody.angularVelocity = Vector3.zero;
			
			await UniTask.WaitForFixedUpdate();
			
			if (this == null)
				return;
			
			ObjectID = "";
			PoolingManager.Instance.Add(ProjectileData, thisGo);
		}
	}
}