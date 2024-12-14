using System;
using Casts;
using Managers;
using Objects;
using UnityEngine;
using Weapons.Base;
using Random = UnityEngine.Random;

namespace Weapons
{
	public class HealthGun : BaseWeapon
	{
		public override Type Cast => typeof(HealthSpring);

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
		
		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			ObjectManager.Instance.CreatePool(typeof(HealthPool), LastHit.point + Vector3.up * 0.06f, Lifetime);
			return true;
		}
	}
}