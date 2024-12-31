using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Base;
using Combat.Enums;
using Managers;
using UnityEngine;

namespace Combat.Attacks
{
	public class Incinerate : BaseAttack
	{
		[SerializeField]
		public float Damage = 100f;
		
		private List<IAlive> alives = new ();
		
		public void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive) || alives.Contains(alive))
				return;
			
			alives.Add(alive);
			alive.Damage(Damage, this, EDamageType.Magic);
		}
		
		public override void OnTriggerEnabled()
		{
			alives.Clear();
			base.OnTriggerEnabled();
		}
	}
}