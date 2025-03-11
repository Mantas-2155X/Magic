using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct ClearPathJob : IJobParallelFor
	{
		public NativeArray<SNode> Nodes;
		
		public void Execute(int index)
		{
			var node = Nodes[index];
			node.GCost = float.MaxValue;
			node.HCost = 0f;
			node.FCost = 0f;
			node.Connection = -1;

			Nodes[index] = node;
		}
	}
}