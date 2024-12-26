using System.Collections.Generic;
using AI.Interfaces;
using Attacks.Base;
using Cysharp.Threading.Tasks;
using Managers;
using Objects;
using UnityEngine;

namespace Attacks
{
	public class TimeSlice : BaseAttack
	{
		[SerializeField]
		public float FadeDuration = 1f;
		
		private float previousTimeScale;
		
		public override void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			base.Spawn(source, position, angles, attach);
			start().Forget();
		}
		
		private async UniTask start()
		{
			var world = World.World.Instance;
			var render = RenderManager.Instance;

			previousTimeScale = world.TimeScale;
			await fade(world, world.TimeScale, 0.1f, render, 0f, 1f);

			var player = AIManager.Instance.Player;
			var npcs = AIManager.Instance.NPCs;
			
			var sourceRelationship = GetAlive()?.RelationshipGroup ?? -99;

			var alives = new List<IAlive>();
			foreach (var npc in npcs)
			{
				if (!npc.IsAlive)
					continue;
				
				if (npc.RelationshipGroup == sourceRelationship)
					continue;
				
				alives.Add(npc);
			}

			if (player.RelationshipGroup != sourceRelationship)
				alives.Add(player);

			foreach (var target in alives)
			{
				var slice = (Slice)ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("Slice"), Vector3.zero, Vector3.zero);
				slice.SetTarget(target);
			}
			
			await UniTask.WaitForSeconds(1.5f, true);

			foreach (var target in alives)
				target.Damage(150, Source);
			
			await UniTask.WaitForSeconds(1f, true);
			
			await fade(world, 0.1f, previousTimeScale, render, 1f, 0f);
			enabled = false;
		}
		
		private async UniTask fade(World.World world, float from, float to, RenderManager render, float invertFrom, float invertTo)
		{
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (world == null || render == null)
					return;
				
				world.TimeScale = Mathf.SmoothStep(from, to, normalizedTime);
				render.InvertColors(Mathf.SmoothStep(invertFrom, invertTo, normalizedTime));
				
				normalizedTime += Time.unscaledDeltaTime / FadeDuration;
			}

			world.TimeScale = to;
		}
	}
}