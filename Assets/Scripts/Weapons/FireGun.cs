using System;
using Casts;
using Projectiles;
using UnityEngine;
using Weapons.Base;
using Random = UnityEngine.Random;

namespace Weapons
{
	public class FireGun : BaseProjectileWeapon
	{
		public override Type Projectile => typeof(FireBall);
		public override Type Cast => typeof(FireRing);

		[SerializeField]
		public Transform[] Rings;
		
		public override void Update()
		{
			base.Update();

			for (var i = 0; i < Rings.Length; i++)
				Rings[i].Rotate(new Vector3(Random.Range(1f, 3f), Random.Range(1f, 3f), Random.Range(1f, 3f)) * (32f * Time.deltaTime));
		}
	}
}