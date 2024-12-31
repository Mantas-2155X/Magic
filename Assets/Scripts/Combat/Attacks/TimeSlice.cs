using AI.Interfaces;
using Combat.Attacks.Base;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Combat.Attacks
{
	public class TimeSlice : BaseAttack
	{
		[SerializeField]
		public float DamageAfter = 1.5f;
		
		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);
			start().Forget();
		}
		
		private async UniTaskVoid start()
		{
			await UniTask.WaitForSeconds(DamageAfter, true);

			if (Target == null || !Target.TryGetComponent<IAlive>(out var alive))
				return;
			
			alive.Damage(AttackData.Damage, GetAlive(), EDamageType.Magic);
		}
	}
}