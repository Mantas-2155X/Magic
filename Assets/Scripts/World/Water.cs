using System.Collections.Generic;
using AI.Interfaces;
using UnityEngine;

namespace World
{
	public class Water : MonoBehaviour
	{
		[SerializeField]
		public int StayDuration = 5;

		[SerializeField]
		public int Damage = 10;

		private List<IAlive> alives = new ();
		
		private int duration;

		public void FixedUpdate()
		{
			duration++;
			
			if (duration < StayDuration)
				return;
			
			duration = 0;

			foreach (var alive in alives)
			{
				if (alive == null || !alive.IsAlive)
					continue;
				
				alive.Damage(Damage, this);
			}
		}
		
		public void OnTriggerEnter(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null || alives.Contains(alive))
				return;
			
			alives.Add(alive);
		}

		public void OnTriggerExit(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null)
				return;

			alives.Remove(alive);
		}

	}
}