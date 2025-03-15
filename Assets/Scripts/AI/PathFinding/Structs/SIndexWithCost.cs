using System;
using Unity.Burst;

namespace AI.PathFinding.Structs
{
	[BurstCompile]
	public struct SIndexWithCost : IEquatable<SIndexWithCost>
	{
		public int Index;
		
		public float Cost;

		public bool Valid;
		
		public bool Equals(SIndexWithCost other)
		{
			return Index == other.Index && Cost.Equals(other.Cost) && Valid == other.Valid;
		}
		
		public override bool Equals(object obj)
		{
			return obj is SIndexWithCost other && Equals(other);
		}
		
		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Index;
				hashCode = (hashCode * 397) ^ Cost.GetHashCode();
				hashCode = (hashCode * 397) ^ Valid.GetHashCode();
				return hashCode;
			}
		}
		
		public static bool operator ==(SIndexWithCost left, SIndexWithCost right)
		{
			return left.Equals(right);
		}
		
		public static bool operator !=(SIndexWithCost left, SIndexWithCost right)
		{
			return !left.Equals(right);
		}
	}
}