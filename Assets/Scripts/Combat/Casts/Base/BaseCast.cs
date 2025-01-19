using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Managers;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Casts.Base
{
	public class BaseCast : MonoBehaviour, ICast
	{
		[field: SerializeField]
		public CastData CastData { get; private set; }

		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }

		private Transform ownerTr;
		
		private GameObject thisGo;
		private Transform thisTr;

		private IAlive owner;

		private bool init;
		
		public void Update()
		{
			if (Source == null)
				return;

			setPosition();
		}
		
		public void OnParticleSystemStopped()
		{
			thisGo.SetActive(false);
		}

		public void OnDisable()
		{
			PoolingManager.Instance.Add(CastData, thisGo);
		}
		
		public void Spawn(Component source)
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

			ownerTr = owner != null ? owner.GetTransform() : Source.transform;
			
			setPosition();
			
			thisGo.SetActive(true);
			System.Play(true);
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
			thisTr.position = ownerTr.position + -ownerTr.up * (0.95f * ownerTr.localScale.y);
		}
	}
}