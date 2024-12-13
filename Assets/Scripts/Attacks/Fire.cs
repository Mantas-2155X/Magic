using AI.Interfaces;
using Attacks.Base;
using UnityEngine;

namespace Attacks
{
	public class Fire : BaseAttack
	{
		public void OnTriggerEnter(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null || !alive.IsAlive)
				return;
			
			alive.Kill(this);
		}
	}
}