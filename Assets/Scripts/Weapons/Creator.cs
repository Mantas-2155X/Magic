using Managers;
using UnityEngine;
using Weapons.Base;

namespace Weapons
{
	public class Creator : BaseWeapon
	{
		public override void FinishCasting()
		{
			base.FinishCasting();
			AIManager.Instance.CreateNPC(LastHit.point + Vector3.up * 1.25f, new Vector3(0, Owner.GetGameObject().transform.eulerAngles.y, 0));
		}
	}
}