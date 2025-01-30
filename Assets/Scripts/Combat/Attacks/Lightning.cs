using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Base;
using UnityEngine;

namespace Combat.Attacks
{
	public class Lightning : BaseAttack
	{
		[SerializeField]
		public List<ParticleSystem> Systems;
		
		private readonly List<IAlive> alives = new ();

		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);
			
			alives.Clear();

			Systems[0].Play(true);
		}
		
		public void OnParticleCollision(GameObject other)
		{
			if (!other.TryGetComponent<IAlive>(out var alive) || !alive.IsAlive || alives.Contains(alive))
				return;
			
			// Don't damage caster
			if (GetAlive() == alive)
				return;
			
			alives.Add(alive);
			
			alive.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
			alive.AddSlowSource(AttackData.Slow.Amount, AttackData.Slow.Duration);
		}
	}
}