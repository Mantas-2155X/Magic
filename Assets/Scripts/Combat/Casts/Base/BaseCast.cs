using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Managers;
using ScriptableObjects;
using State.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.Casts.Base
{
	public class BaseCast : MonoBehaviour, ICast
	{
		[field: SerializeField]
		public CastData CastData { get; private set; }

		public IIdentifiable Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }

		private Transform ownerTr;
		
		private GameObject thisGo;
		private Transform thisTr;

		private IAlive owner;

		private bool init;
		
		#region Identify / SaveLoad

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
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
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Source.IsNull())
				return;

			setPosition();
		}
		
		public void OnParticleSystemStopped()
		{
			thisGo.SetActive(false);
		}

		public void OnDisable()
		{
			StateManager.Instance.UnregisterSavedItem(ObjectID);
			
			ObjectID = "";
			PoolingManager.Instance.Add(CastData, thisGo);
		}
		
		public void Spawn(IIdentifiable source)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Casts);
				init = true;
			}
			
			Source = source;
			
			owner = null;
			owner = GetAlive();

			ownerTr = owner.NotNull() ? owner.GetTransform() : Source.GetTransform();
			
			setPosition();
			
			thisGo.SetActive(true);
			System.Play(true);
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

		public void StopParticles()
		{
			System.Stop(true, ParticleSystemStopBehavior.StopEmitting);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private void setPosition()
		{
			var newPos = ownerTr.position + -ownerTr.up * (0.95f * ownerTr.localScale.y);

			if (owner.NotNull())
				newPos.y = owner.Body.CanSway ? owner.Body.Feet[0].position.y : owner.Body.Core.position.y;
			
			thisTr.position = newPos;
		}
	}
}