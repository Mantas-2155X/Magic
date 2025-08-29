using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using SteamAudio;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace Combat.Attacks
{
	public class Lightning : BaseAttack
	{
		[SerializeField]
		public List<ParticleSystem> Systems;
		
		[SerializeField]
		public int DecalsPerSystem = 2;

		[SerializeField]
		public int SoundsPerSystem = 1;

		private readonly List<IAlive> alives = new ();
		private readonly List<IObject> objects = new ();
		
		private readonly List<ParticleCollisionEvent> collisions = new ();
		
		private readonly Dictionary<ParticleSystem, int> systemDecals = new ();
		private readonly Dictionary<ParticleSystem, int> systemSounds = new ();

		public override void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f)
		{
			base.Spawn(source, position, angles, attach, elapsedTime);
			
			alives.Clear();
			objects.Clear();

			for (var i = 0; i < Systems.Count; i++)
			{
				var system = Systems[i];
				
				systemDecals[system] = 0;
				systemSounds[system] = 0;
			}

			if (elapsedTime > 0f)
				Systems[0].Simulate(elapsedTime);
			
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
					if (alive.Data.AttachDecals)
						attach = alive;
					
					alives.Add(alive);
			
					alive.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
					alive.AddParalyzeSource(ObjectID, AttackData.Paralyze.Duration);
				}
			}
			else if (other.TryGetComponent<IObject>(out var obj) && !objects.Contains(obj))
			{
				if (obj.ObjectData.AttachDecals)
					attach = obj;
				
				objects.Add(obj);
				
				obj.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
			}
			
			foreach (var system in Systems)
			{
				var decals = systemDecals[system];
				if (decals < DecalsPerSystem)
				{
					var eventsCount = system.GetCollisionEvents(other, collisions);
					if (eventsCount == 0)
						continue;

					var clamped = Mathf.Clamp(eventsCount, 0, DecalsPerSystem - decals);

					for (var i = 0; i < clamped; i++)
						ObjectManager.Instance.CreateDecal(ObjectManager.Instance.GetData<DecalData>("DECALS_SMALLDECAL_NAME"), collisions[i], attach);

					systemDecals[system] = decals + clamped;
				}

				var sounds = systemSounds[system];
				if (sounds < SoundsPerSystem)
				{
					var eventsCount = system.GetCollisionEvents(other, collisions);
					if (eventsCount == 0)
						continue;

					var clamped = Mathf.Clamp(eventsCount, 0, SoundsPerSystem - sounds);

					for (var i = 0; i < clamped; i++)
					{
						var collision = collisions[i];
						
						var geometry = collision.colliderComponent.GetComponentInChildren<SteamAudioGeometry>();
						if (geometry != null)
							AudioManager.Instance.PlayImpact(geometry.material, collision.intersection + (collision.normal * 0.1f));
					}

					systemSounds[system] = sounds + clamped;
				}
			}
		}
	}
}