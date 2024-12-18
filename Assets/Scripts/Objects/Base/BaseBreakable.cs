using System;
using System.Runtime.CompilerServices;
using Objects.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseBreakable : MonoBehaviour, IBreakable
	{
		[field: SerializeField]
		public float Health { get; private set; }

		public bool IsBroken { get; private set; }
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		public void Awake()
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				init = true;
			}
		}

		public virtual void Damage(float damage, object source)
		{
			if (IsBroken || damage < 0)
				return;
			
			Health -= damage;

			if (Health > 0)
				return;
			
			Break(source);
		}
		
		public virtual void Break(object source)
		{
			if (IsBroken)
				return;

			Health = 0;
			IsBroken = true;
			
			Destroy(thisGo);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
	}
}