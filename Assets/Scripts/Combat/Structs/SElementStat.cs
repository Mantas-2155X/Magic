using System;
using UnityEngine;

namespace Combat.Structs
{
	[Serializable]
	public struct SElementStat
	{
		[SerializeField]
		public float Constant;
		
		[SerializeField]
		public float Percentage;

		public void Append(SElementStat stat)
		{
			Constant += stat.Constant;
			Percentage += stat.Percentage;
		}

		public float Convert(float original)
		{
			// First add the percentage of the current value depending on the stat
			original += original * (Percentage / 100f);
			
			// Then add the constant value
			original += Constant;

			return original;
		}
	}
}