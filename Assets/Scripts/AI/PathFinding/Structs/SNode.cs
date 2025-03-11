using System;
using AI.PathFinding.Enums;
using Unity.Burst;
using Unity.Mathematics;

namespace AI.PathFinding.Structs
{
	[BurstCompile]
	public struct SNode : IEquatable<SNode>
	{
		#region Node

		public int Index;
			
		public int3 GridPosition;
		public float3 WorldPosition;

		public ENodeAvailabilityFlags Availability;
			
		#endregion
			
		#region Pathing

		public float GCost;
		public float HCost;
		public float FCost;

		public int Connection;

		#endregion
			
		public bool Equals(SNode other)
		{
			return Index == other.Index;
		}
			
		public override bool Equals(object obj)
		{
			return obj is SNode other && Equals(other);
		}
			
		public override int GetHashCode()
		{
			return Index;
		}
			
		public static bool operator ==(SNode left, SNode right)
		{
			return left.Equals(right);
		}
			
		public static bool operator !=(SNode left, SNode right)
		{
			return !left.Equals(right);
		}
	}
}