using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using Objects.Interfaces;
using State.Interfaces;
using UnityEngine;

namespace Combat.Attacks
{
	public class Lightning : BaseAttack
	{
		[SerializeField]
		public List<ParticleSystem> Systems;
		
		[SerializeField]
		public int DecalsPerSystem = 2;

		private readonly List<IAlive> alives = new ();
		private readonly List<IObject> objects = new ();
		
		private readonly List<ParticleCollisionEvent> collisions = new ();
		private readonly Dictionary<ParticleSystem, int> systemDecals = new ();

		public override void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach)
		{
			base.Spawn(source, position, angles, attach);
			
			alives.Clear();
			objects.Clear();

			for (var i = 0; i < Systems.Count; i++)
				systemDecals[Systems[i]] = 0;

			Systems[0].Play(true);
		}
		
		public void OnParticleCollision(GameObject other)
		{
			IIdentifiable attach = null;
			
			if (other.TryGetComponent<IAlive>(out var alive) && !alives.Contains(alive))
			{
				// Don't damage caster
				if (GetAlive() != alive)
				{
					attach = alive;
					alives.Add(alive);
			
					alive.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
					alive.AddParalyzeSource(ObjectID, AttackData.Paralyze.Duration);
				}
			}
			else if (other.TryGetComponent<IObject>(out var obj) && !objects.Contains(obj))
			{
				attach = obj;
				objects.Add(obj);
				
				obj.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
			}
			
			foreach (var system in Systems)
			{
				var decals = systemDecals[system];
				if (decals >= DecalsPerSystem)
					continue;
				
				var eventsCount = system.GetCollisionEvents(other, collisions);
				if (eventsCount == 0)
					continue;

				var clamped = Mathf.Clamp(eventsCount, 0, DecalsPerSystem - decals);
					
				for (var i = 0; i < clamped; i++)
					ObjectManager.Instance.CreateDecal(ObjectManager.Instance.GetDecal("DECALS_SMALLDECAL_NAME"), collisions[i], attach);

				systemDecals[system] = decals + clamped;
			}
		}
	}
}