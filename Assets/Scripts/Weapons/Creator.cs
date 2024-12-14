using Managers;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Creator : BaseWeapon
	{
		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;
			
			AIManager.Instance.CreateNPC(LastHit.point + Vector3.up * 1.25f, new Vector3(0, Owner.GetGameObject().transform.eulerAngles.y, 0));
			return true;
		}
	}
}