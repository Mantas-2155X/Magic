using System.Collections.Generic;
using AI.Interfaces;
using Attacks.Base;
using Managers;
using UnityEngine;

namespace Attacks
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
			alive.Damage(Damage, this);
		}
		
		public override void OnTriggerEnabled()
		{
			alives.Clear();
			base.OnTriggerEnabled();
		}
	}
}