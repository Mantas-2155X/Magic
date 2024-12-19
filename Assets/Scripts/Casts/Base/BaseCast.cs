using System.Runtime.CompilerServices;
using Casts.Interfaces;
using Managers;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Base
{
	public class BaseCast : MonoBehaviour, ICast
	{
		[field: SerializeField]
		public CastData CastData { get; set; }

		public Component Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }

		private Transform ownerTr;
		
		private GameObject thisGo;
		private Transform thisTr;

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
			PoolingManager.Instance.AddToPool(CastData, thisGo);
		}
		
		public void Spawn(Component source)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Other);
				init = true;
			}
			
			Source = source;
			
			if (Source is IWeapon weapon && weapon.Owner != null)
				ownerTr = weapon.Owner.GetTransform();
			else
				ownerTr = Source.transform;
			
			setPosition();
			
			thisGo.SetActive(true);
			System.Play(true);
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
			thisTr.position = ownerTr.position + Vector3.down * 0.95f;
		}
	}
}