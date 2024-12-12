using AI.Interfaces;
using Tools;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Destroyer : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();

			if (!Physics.Raycast(FinishedRay, out var hit, float.MaxValue, ~LayerMaskTools.Mask1))
				return;
			
			var alive = hit.collider.GetComponent<IAlive>();
			alive?.Kill(this);
		}
	}
}