using AI.Interfaces;
using Objects;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Spells.Interfaces
{
	public interface ISpell : IIdentifiable
	{
		public SpellData SpellData { get; set; }

		public IAlive Owner { get; set; }

		public Ray LastRay { get; }
		public RaycastHit LastHit { get; }

		public float OverrideRange { get; set; }
		
		public bool IsCasting { get; }
		public bool IsSelected { get; }
		public bool IsOnCooldown { get; }

		public float LastStartedCast { get; }
		public float LastFinishedCast { get; }
		public float PredictFinishCast { get; }
		
		public void Select();
		public void Unselect();
		
		public bool CanCast();
		
		public void StartCasting();
		public bool FinishCasting();
		public void CancelCasting();
	}
}