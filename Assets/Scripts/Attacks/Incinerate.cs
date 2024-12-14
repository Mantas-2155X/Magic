using System.Collections.Generic;
using AI.Interfaces;
using Attacks.Base;
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
			var alive = other.GetComponent<IAlive>();
			if (alive == null || !alive.IsAlive || alives.Contains(alive))
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