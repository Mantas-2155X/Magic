using Managers;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Creator : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;

			AIManager.Instance.CreateNPC(hit.point + Vector3.up * 1.25f, new Vector3(0, Owner.GetGameObject().transform.eulerAngles.y, 0));
		}
	}
}