using System.Collections.Generic;

namespace AI.Enums
{
	public enum EAIMode
	{
		Idle,
		Walking,
		Action
	}
	
	public struct EAIModeComparer : IEqualityComparer<EAIMode>
	{
		public bool Equals(EAIMode x, EAIMode y)
		{
			return x == y;
		}

		public int GetHashCode(EAIMode obj)
		{
			return (int)obj;
		}
	}
}