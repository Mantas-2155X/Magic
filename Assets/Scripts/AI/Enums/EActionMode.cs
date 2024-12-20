using System.Collections.Generic;

namespace AI.Enums
{
	public enum EActionMode
	{
		None,
		WanderAggressively,
		PatrolAggressively,
		WaitAggressively,
	}
	
	public struct EActionModeComparer : IEqualityComparer<EActionMode>
	{
		public bool Equals(EActionMode x, EActionMode y)
		{
			return x == y;
		}

		public int GetHashCode(EActionMode obj)
		{
			return (int)obj;
		}
	}
}