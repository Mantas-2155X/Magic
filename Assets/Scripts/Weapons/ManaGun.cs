using Managers;
using Objects;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class ManaGun : BaseWeapon
	{
		[SerializeField]
		public Transform[] Rings;
		
		public override void Update()
		{
			base.Update();

			for (var i = 0; i < Rings.Length; i++)
				Rings[i].Rotate(new Vector3(Random.Range(1f, 3f), Random.Range(1f, 3f), Random.Range(1f, 3f)) * (32f * Time.deltaTime));
		}
		
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;

			ObjectManager.Instance.CreatePool(typeof(ManaPool), hit.point + Vector3.up * 0.06f);
		}
	}
}