using Managers;
using Objects;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class HealthGun : BaseWeapon
	{
		[SerializeField]
		public Transform[] Rings;
		
		public void Update()
		{
			for (var i = 0; i < Rings.Length; i++)
			{
				Rings[i].Rotate(new Vector3(Random.Range(1f, 3f), Random.Range(1f, 3f), Random.Range(1f, 3f)) * (32f * Time.deltaTime));
			}
		}
		
		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;

			if (!Physics.Raycast(Ray, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return false;

			ObjectManager.Instance.CreatePool(typeof(HealthPool), hit.point + Vector3.up * 0.06f);
			return true;
		}
	}
}