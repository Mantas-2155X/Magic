using System.Collections.Generic;
using AI.Interfaces;
using Combat.Spells.Base;
using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

namespace Combat.Spells
{
	public class TimeSlicer : BaseSpell
	{
		[SerializeField]
		public float FadeDuration = 1f;

		[SerializeField]
		public float ReturnAfter = 2.5f;

		public static bool Active { get; private set; }
		
		private readonly List<IAlive> targets = new ();

		private float previousTimeScale;

		public override bool FinishCasting()
		{
			if (Active)
			{
				CancelCasting();
				return false;
			}
			
			var status = base.FinishCasting();
			if (!status)
				return false;

			Active = true;
			start().Forget();
			
			return true;
		}

		public override bool CanCast()
		{
			return base.CanCast() && !Active;
		}
		
		private async UniTaskVoid start()
		{
			var render = RenderManager.Instance;
			
			var player = AIManager.Instance.Player;
			var npcs = AIManager.Instance.NPCs;

			var ownerRelationship = Owner.RelationshipGroup;

			previousTimeScale = GameManager.TimeScale;
			
			await fade(previousTimeScale, previousTimeScale * 0.1f, render, 0f, 1f);
			
			targets.Clear();

			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc.RelationshipGroup == ownerRelationship)
					continue;
				
				targets.Add(npc);
			}

			if (player.IsAlive && player.RelationshipGroup != ownerRelationship)
				targets.Add(player);

			for (var i = 0; i < targets.Count; i++)
				ObjectManager.Instance.CreateAttack(ObjectManager.Instance.GetAttack("ATTACK_TIMESLICE_NAME"), this, targets[i].GetTransform());
			
			await UniTask.WaitForSeconds(ReturnAfter, true);
			
			await fade(previousTimeScale * 0.1f, previousTimeScale, render, 1f, 0f);

			Active = false;
		}
		
		private async UniTask fade(float from, float to, RenderManager render, float invertFrom, float invertTo)
		{
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (this == null)
					return;
				
				GameManager.TimeScale = Mathf.SmoothStep(from, to, normalizedTime);
				render.InvertColors(Mathf.SmoothStep(invertFrom, invertTo, normalizedTime));
				
				normalizedTime += Time.unscaledDeltaTime / FadeDuration;
			}

			GameManager.TimeScale = to;
			render.InvertColors(invertTo);
		}
	}
}