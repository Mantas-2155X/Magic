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

		public void AppendStat(SElementStat stat)
		{
			Constant += stat.Constant;
			Percentage += stat.Percentage;
		}

		public void Add(ref float original)
		{
			// First add the percentage of the current value depending on the stat
			original += original * (Percentage / 100f);
			
			// Then add the constant value
			original += Constant;
		}
		
		public void Subtract(ref float original)
		{
			// First subtract the percentage of the current value depending on the stat
			original -= original * (Percentage / 100f);
			
			// Then subtract the constant value
			original -= Constant;
		}
	}
}