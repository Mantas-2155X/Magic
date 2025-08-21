using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using AI;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Combat.Projectiles.Base
{
	public class BaseProjectile : MonoBehaviour, IProjectile
	{
		[field: SerializeField]
		public ProjectileData ProjectileData { get; private set; }

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

		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => true;
		
		public virtual bool ExternallySpawned { get; set; }

		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
		public virtual ELoadType LoadType => ELoadType.Create;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Late;

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			var createData = new ProjectileCreateData()
			{
				Name = ProjectileData.Name,
				Range = SpellRange,
				Attack = AttackData != null ? AttackData.Name : null,
				SourceObjectID = Source.NotNull() ? Source.ObjectID : null,
				ElapsedTime = Time.time - CreatedTime,
				States = GetModifications()
			};

			return JObject.FromObject(createData);
		}
		
		public static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<ProjectileCreateData>();
			
			var obj = (BaseProjectile)ObjectManager.Instance.CreateProjectile(ObjectManager.Instance.GetData<ProjectileData>(createData.Name), createData.Range, ObjectManager.Instance.GetData<AttackData>(createData.Attack), StateManager.Instance.GetRegisteredObject(createData.SourceObjectID), Vector3.zero, Vector3.zero, createData.ElapsedTime);
			obj.ObjectID = data.Item1;
			
			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[BaseProjectile] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(Transform).ToString()] = JObject.FromObject(new TransformState(thisTr));
			dict[typeof(Rigidbody).ToString()] = JObject.FromObject(new RigidbodyState(Rigidbody));
			dict[typeof(BaseProjectile).ToString()] = JObject.FromObject(new BaseProjectileState(this));

			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState) && transformState != null)
				transformState.ToObject<TransformState>().Apply(thisTr);
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState) && rigidbodyState != null)
				rigidbodyState.ToObject<RigidbodyState>().Apply(Rigidbody);
			
			if (data.TryGetValue(typeof(BaseProjectile).ToString(), out var baseProjectileState) && baseProjectileState != null)
				baseProjectileState.ToObject<BaseProjectileState>().Apply(this);
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

					// Slight random push for flying npcs
					if (alive is NPC npc && npc.Agent.HasFlight)
					{
						var direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.25f, 0.25f), Random.Range(-1f, 1f));
						
						var npcRigidBody = npc.Body.Rigidbody;
						npcRigidBody.AddForce(direction * (npcRigidBody.mass * 1.2f), ForceMode.Impulse);
						
						npc.Chase.ResetChaseRange(false);
					}
				}
				else if (coll.TryGetComponent<IObject>(out var obj))
				{
					attach = obj;
					obj.Damage(ProjectileData.Damage, GetAlive(), ProjectileData.Element, contact.point);
				}
			}
			
			if (AttackData != null)
				ObjectManager.Instance.CreateAttack(AttackData, Source, contact, attach);

			if (ProjectileData.Decal != null)
			{
				if (attach.IsNull() || ((attach is not IObject obj || obj.ObjectData.AttachDecals) && (attach is not IAlive alive || alive.Data.AttachDecals)))
				{
					ObjectManager.Instance.CreateDecal(ProjectileData.Decal, contact, attach);
				}
			}
				
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
			
				ignoreBodyCollider = body.HitboxCollider;
				Physics.IgnoreCollision(ignoreBodyCollider, Collider, true);

				if (body.MovementCollider != null)
				{
					ignoreFeetCollider = body.MovementCollider;
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
		
		[JsonObject]
		public class BaseProjectileState : IState
		{
			[JsonProperty]
			public Vector3 StartingPosition;
			
			public BaseProjectileState() { }
			
			public BaseProjectileState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not BaseProjectile baseProjectile)
					return;

				StartingPosition = baseProjectile.StartingPosition;
			}
			
			public void Apply(object obj)
			{
				if (obj is not BaseProjectile baseProjectile)
					return;

				baseProjectile.SetState(StartingPosition);
			}
		}
		
		public class ProjectileCreateData : CreateData
		{
			[JsonProperty]
			public float Range;

			[JsonProperty]
			public string Attack;

			[JsonProperty]
			public string SourceObjectID;
		
			[JsonProperty]
			public float ElapsedTime;
		}
	}
}