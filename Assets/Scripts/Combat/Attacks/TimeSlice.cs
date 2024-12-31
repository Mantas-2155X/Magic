using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Base;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Objects;
using UnityEngine;

namespace Combat.Attacks
{
	public class TimeSlice : BaseAttack
	{
		[SerializeField]
		public float FadeDuration = 1f;
		
		public static bool Active { get; private set; }
		
		private readonly List<IAlive> targets = new ();

		private float previousTimeScale;
		
		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			Active = true;
			base.Spawn(source, position, angles, attach);
			start().Forget();
		}
		
		public override void OnDisable()
		{
			Active = false;
			base.OnDisable();
		}
		
		private async UniTaskVoid start()
		{
			var world = World.World.Instance;
			var render = RenderManager.Instance;
			
			var player = AIManager.Instance.Player;
			var npcs = AIManager.Instance.NPCs;

			var sourceRelationship = GetAlive()?.RelationshipGroup ?? -99;

			previousTimeScale = world.TimeScale;
			
			await fade(world, world.TimeScale, 0.1f, render, 0f, 1f);
			
			targets.Clear();

			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc.RelationshipGroup == sourceRelationship)
					continue;
				
				targets.Add(npc);
			}

			if (player.IsAlive && player.RelationshipGroup != sourceRelationship)
				targets.Add(player);

			for (var i = 0; i < targets.Count; i++)
			{
				var slice = (Slice)ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("Slice"), Vector3.zero, Vector3.zero);
				slice.SetTarget(targets[i]);
			}
			
			await UniTask.WaitForSeconds(1.5f, true);

			foreach (var target in targets)
				target.Damage(150, Source, EDamageType.Magic);
			
			await UniTask.WaitForSeconds(1f, true);
			
			await fade(world, 0.1f, previousTimeScale, render, 1f, 0f);
			
			GetGameObject().SetActive(false);
		}
		
		private async UniTask fade(World.World world, float from, float to, RenderManager render, float invertFrom, float invertTo)
		{
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				world.TimeScale = Mathf.SmoothStep(from, to, normalizedTime);
				render.InvertColors(Mathf.SmoothStep(invertFrom, invertTo, normalizedTime));
				
				normalizedTime += Time.unscaledDeltaTime / FadeDuration;
			}

			world.TimeScale = to;
			render.InvertColors(invertTo);
		}
	}
}