using System;
using Unity.Burst;

namespace AI.PathFinding.Structs
{
	[BurstCompile]
	public struct SIndexWithCost : IEquatable<SIndexWithCost>
	{
		public int Index;
		
		public float Cost;

		public bool Connects;
		
		public bool Equals(SIndexWithCost other)
		{
			return Index == other.Index && Cost.Equals(other.Cost) && Connects == other.Connects;
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
				hashCode = (hashCode * 397) ^ Connects.GetHashCode();
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