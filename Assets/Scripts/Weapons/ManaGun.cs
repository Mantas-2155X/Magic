using System;
using Casts;
using Managers;
using Objects;
using UnityEngine;
using Weapons.Base;
using Random = UnityEngine.Random;

namespace Weapons
{
	public class ManaGun : BaseWeapon
	{
		public override Type Cast => typeof(ManaSpring);

		[SerializeField]
		public Transform[] Rings;
		
		[SerializeField]
		public float Lifetime = 15f;

		public override void Update()
		{
			base.Update();

			for (var i = 0; i < Rings.Length; i++)
				Rings[i].Rotate(new Vector3(Random.Range(1f, 3f), Random.Range(1f, 3f), Random.Range(1f, 3f)) * (32f * Time.deltaTime));
		}
		
		public override void FinishCasting()
		{
			base.FinishCasting();
			ObjectManager.Instance.CreatePool(typeof(ManaPool), LastHit.point + Vector3.up * 0.06f, Lifetime);
		}
	}
}