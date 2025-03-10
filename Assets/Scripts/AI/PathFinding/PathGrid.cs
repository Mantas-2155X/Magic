using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using AI.PathFinding.Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AI.PathFinding
{
	[ExecuteInEditMode]
	public class PathGrid : MonoBehaviour
	{
		[Header("Grid Settings")]
		[SerializeField]
		public Vector3 Offset = Vector3.zero;
		[SerializeField]
		public Vector3 Size = Vector3.one;
		
		[SerializeField][Range(0.1f, 10f)]
		public float Distance = 1f;

		[SerializeField][Range(0.01f, 5f)]
		public float Radius = 0.1f;

		[Header("Filter Settings")]
		[SerializeField]
		public LayerMask FilterMask = -1;

		[Header("Draw Settings")]
		[SerializeField]
		public bool DrawBounds = true;
		[SerializeField]
		public bool DrawNodes = true;
		[SerializeField]
		public bool DrawConnections = true;
		[SerializeField]
		public bool DrawPath = true;
		
		public ENodeAvailabilityFlags DrawFlags = (ENodeAvailabilityFlags)~0;
		
		[Header("Job Settings")]
		public int BatchCount = 64;
		
		[Header("Path Finding")]
		[SerializeField][Range(0.5f, 1f)]
		public float Accuracy = 0.75f;
		[SerializeField]
		public Vector3 Start;
		[SerializeField]
		public Vector3 End;

		private NativeArray<SNode> nodes;
		private NativeArray<SIndexWithCost> neighbors;
		
		private NativeHashSet<int> searchedNodes;
		private NativeList<int> toSearchNodes;
		
		private NativeList<SNode> resultingPath;
		
		private int xSize;
		private int ySize;
		private int zSize;

		#region MonoBehaviour

		public void Update()
		{
			var createStopwatch = new Stopwatch();
			createStopwatch.Start();
			CreateGrid();
			createStopwatch.Stop();
			Debug.Log($"Creating grid [job] (size {xSize * ySize * zSize}) took {createStopwatch.ElapsedMilliseconds}ms");
			
			var findStopwatch = new Stopwatch();
			findStopwatch.Start();
			FindPath();
			findStopwatch.Stop();
			Debug.Log($"Finding path [job] took {findStopwatch.ElapsedMilliseconds}ms");
		}

		public void OnDestroy()
		{
			cleanup();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (!nodes.IsCreated)
				return;
			
			if (DrawBounds)
				Gizmos.DrawWireCube(transform.position + Offset, Size);

			if (DrawNodes)
			{
				for (var i = 0; i < nodes.Length; i++)
				{
					var node = nodes[i];
					if ((node.Availability & DrawFlags) == 0)
						continue;

					if (node.Availability == ENodeAvailabilityFlags.Available)
					{
						Gizmos.color = Color.green;
					}
					else if ((node.Availability & ENodeAvailabilityFlags.InsideObject) != 0)
					{
						Gizmos.color = Color.red;
					}
					else if ((node.Availability & ENodeAvailabilityFlags.NoConnections) != 0)
					{
						Gizmos.color = Color.yellow;
					}
							
					if (searchedNodes.Contains(i))
						Gizmos.color = Color.black;
					
					Gizmos.DrawSphere(node.WorldPosition, Radius);
				}
			}

			if (DrawConnections)
			{
				Gizmos.color = new Color(1f, 0.5f, 0f);

				for (var i = 0; i < nodes.Length; i++)
				{
					var nodePos = nodes[i].WorldPosition;
					
					var startIndex = i * 26;
					
					for (var k = startIndex; k < startIndex + 26; k++)
					{
						var neighbor = neighbors[k];
						if (!neighbor.Valid)
							continue;
						
						Gizmos.DrawLine(nodePos, nodes[neighbor.Index].WorldPosition);
					}
				}
			}
			
			if (DrawPath && resultingPath.IsCreated && resultingPath.Length > 0)
			{
				Gizmos.color = Color.cyan;
				
				for (var i = 0; i < resultingPath.Length - 1; i++)
					Gizmos.DrawLine(resultingPath[i].WorldPosition, resultingPath[i + 1].WorldPosition);
			}
		}
#endif

		private void cleanup()
		{
			if (nodes.IsCreated)
				nodes.Dispose();
			
			if (neighbors.IsCreated)
				neighbors.Dispose();

			if (searchedNodes.IsCreated)
				searchedNodes.Dispose();
			
			if (toSearchNodes.IsCreated)
				toSearchNodes.Dispose();
			
			if (resultingPath.IsCreated)
				resultingPath.Dispose();
		}
		
		#endregion

		#region Path Grid

		[ContextMenu("Create Grid")]
		public void CreateGrid()
		{
			cleanup();
			
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			var nodesLength = xSize * ySize * zSize;
			var neighborsLength = 26 * nodesLength;

			nodes = new NativeArray<SNode>(nodesLength, Allocator.Persistent);
			neighbors = new NativeArray<SIndexWithCost>(neighborsLength, Allocator.Persistent);

			searchedNodes = new NativeHashSet<int>(nodesLength, Allocator.Persistent);
			toSearchNodes = new NativeList<int>(nodesLength, Allocator.Persistent);

			resultingPath = new NativeList<SNode>(Allocator.Persistent);
			
			#region Initialize Nodes

			var initializeNodesJob = new InitializeNodesJob
			{
				Nodes = nodes,
				Position = transform.position + Offset - Size / 2f,
				Distance = Distance,
				XSize = xSize,
				YSize = ySize,
				ZSize = zSize
			};

			var initializeNodesHandle = initializeNodesJob.Schedule();

			#endregion
			
			#region Filter Overlaps
			
			var overlapCommands = new NativeArray<OverlapSphereCommand>(nodesLength, Allocator.TempJob);
			var overlapResults = new NativeArray<ColliderHit>(nodesLength, Allocator.TempJob);

			var initializeOverlapsJob = new InitializeOverlapsJob
			{
				Nodes = nodes,
				Commands = overlapCommands,
				Radius = Radius,
				Query = new QueryParameters(FilterMask)
			};

			var initializeOverlapsHandle = initializeOverlapsJob.Schedule(nodesLength, BatchCount, initializeNodesHandle);
			
			var overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapResults, BatchCount, 1, initializeOverlapsHandle);

			var filterOverlapsJob = new FilterOverlapsJob
			{
				Nodes = nodes,
				Hits = overlapResults
			};

			var filterOverlapsHandle = filterOverlapsJob.Schedule(overlapResults.Length, BatchCount, overlapHandle);
			
			#endregion

			#region Initialize Neighbors

			var initializeNeighborsJob = new InitializeNeighborsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Distance = Distance,
				Accuracy = Accuracy,
				XSize = xSize,
				YSize = ySize,
				ZSize = zSize
			};

			var initializeNeighborsHandle = initializeNeighborsJob.Schedule(nodesLength, BatchCount, filterOverlapsHandle);

			#endregion

			#region Filter Raycasts

			var raycastCommands = new NativeArray<RaycastCommand>(neighborsLength, Allocator.TempJob);
			var raycastResults = new NativeArray<RaycastHit>(neighborsLength, Allocator.TempJob);

			var initializeRaycastsJob = new InitializeRaycastsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Commands = raycastCommands,
				Query = new QueryParameters(FilterMask, hitBackfaces: true)
			};

			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(nodesLength, BatchCount, initializeNeighborsHandle);
			
			var raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, BatchCount, 1, initializeRaycastsHandle);

			var filterRaycastsJob = new FilterRaycastsJob
			{
				Neighbors = neighbors,
				Hits = raycastResults
			};

			var filterRaycastsHandle = filterRaycastsJob.Schedule(raycastResults.Length, BatchCount, raycastHandle);
			filterRaycastsHandle.Complete();
			
			#endregion

			overlapCommands.Dispose();
			overlapResults.Dispose();
			raycastCommands.Dispose();
			raycastResults.Dispose();
		}

		[ContextMenu("Find Path")]
		public void FindPath()
		{
			var findPathJob = new FindPathJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				ResultingPath = resultingPath,
				SearchedNodes = searchedNodes,
				ToSearchNodes = toSearchNodes,
				Distance = Distance,
				StartPosition = Start,
				EndPosition = End
			};

			var findPathHandle = findPathJob.Schedule();
			findPathHandle.Complete();
		}
		
		#endregion

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

		[BurstCompile]
		public struct SIndexWithCost : IEquatable<SIndexWithCost>
		{
			public int Index;
			
			public float Cost;

			public bool Valid;
			
			public static SIndexWithCost Create(int index, float cost)
			{
				return new SIndexWithCost
				{
					Index = index,
					Cost = cost,
					Valid = true
				};
			}
			
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

		[BurstCompile]
		public struct InitializeNodesJob : IJob
		{
			[WriteOnly]
			public NativeArray<SNode> Nodes;

			public float3 Position;

			public float Distance;
			
			public int XSize;
			public int YSize;
			public int ZSize;

			public void Execute()
			{
				var index = 0;
				
				for (var x = 0; x < XSize; x++)
				{
					for (var y = 0; y < YSize; y++)
					{
						for (var z = 0; z < ZSize; z++)
						{
							int3 gridPosition;
							gridPosition.x = x;
							gridPosition.y = y;
							gridPosition.z = z;

							float3 worldPosition;
							worldPosition.x = x * Distance + Position.x;
							worldPosition.y = y * Distance + Position.y;
							worldPosition.z = z * Distance + Position.z;
						
							Nodes[index] = new SNode
							{
								Index = index,
								GridPosition = gridPosition,
								WorldPosition = worldPosition,
								Availability = ENodeAvailabilityFlags.Available,
								GCost = float.MaxValue,
								HCost = 0f,
								FCost = 0f,
								Connection = -1
							};
						
							index++;
						}
					}
				}
			}
		}

		[BurstCompile]
		public struct InitializeOverlapsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<SNode> Nodes;
			
			[WriteOnly]
			public NativeArray<OverlapSphereCommand> Commands;
			
			public float Radius;

			public QueryParameters Query;
			
			public void Execute(int index)
			{
				Commands[index] = new OverlapSphereCommand(Nodes[index].WorldPosition, Radius, Query);
			}
		}

		[BurstCompile]
		public struct FilterOverlapsJob : IJobParallelFor
		{
			public NativeArray<SNode> Nodes;

			[ReadOnly]
			public NativeArray<ColliderHit> Hits;

			public void Execute(int index)
			{
				if (Hits[index].instanceID == 0)
					return;

				var node = Nodes[index];
				
				var availability = node.Availability;
				availability &= ~ENodeAvailabilityFlags.Available;
				availability |= ENodeAvailabilityFlags.InsideObject;

				node.Availability = availability;
				Nodes[index] = node;
			}
		}
		
		[BurstCompile]
		public struct InitializeNeighborsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<SNode> Nodes;

			[WriteOnly][NativeDisableParallelForRestriction]
			public NativeArray<SIndexWithCost> Neighbors;
			
			public float Distance;
			public float Accuracy;

			public int XSize;
			public int YSize;
			public int ZSize;
			
			public void Execute(int index)
			{
				var node = Nodes[index];
				
				var gridPos = node.GridPosition;
				var worldPos = node.WorldPosition;
				
				var x = gridPos.x;
				var y = gridPos.y;
				var z = gridPos.z;

				var addIndex = index * 26;
				
				for (var cX = -1; cX < 2; cX++)
				{
					for (var cY = -1; cY < 2; cY++)
					{
						for (var cZ = -1; cZ < 2; cZ++)
						{
							var neighborX = x + cX;
							if (neighborX < 0 || neighborX >= XSize)
								continue;
						
							var neighborY = y + cY;
							if (neighborY < 0 || neighborY >= YSize)
								continue;

							var neighborZ = z + cZ;
							if (neighborZ < 0 || neighborZ >= ZSize)
								continue;

							var neighborIndex = getNodeIndex(neighborX, neighborY, neighborZ);
							if (neighborIndex == index)
								continue;

							var cost = math.distance(worldPos, Nodes[neighborIndex].WorldPosition) / Distance;
							var neighbor = SIndexWithCost.Create(neighborIndex, cost * Accuracy);
							
							Neighbors[addIndex] = neighbor;
							addIndex++;
						}
					}
				}
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int getNodeIndex(int x, int y, int z)
			{
				return x * (ZSize * YSize) + y * ZSize + z;
			}
		}

		[BurstCompile]
		public struct InitializeRaycastsJob : IJobParallelFor
		{
			[ReadOnly]
			public NativeArray<SNode> Nodes;
			
			[ReadOnly]
			public NativeArray<SIndexWithCost> Neighbors;
			
			[WriteOnly][NativeDisableParallelForRestriction]
			public NativeArray<RaycastCommand> Commands;

			public QueryParameters Query;
			
			public void Execute(int index)
			{
				var node = Nodes[index];
				var nodePos = node.WorldPosition;

				var startIndex = index * 26;

				for (var i = startIndex; i < startIndex + 26; i++)
				{
					var neighbor = Neighbors[i];
					var neighborPos = Nodes[neighbor.Index].WorldPosition;

					Vector3 nodePosVector;
					nodePosVector.x = nodePos.x;
					nodePosVector.y = nodePos.y;
					nodePosVector.z = nodePos.z;
					
					Vector3 directionVector;
					directionVector.x = neighborPos.x - nodePos.x;
					directionVector.y = neighborPos.y - nodePos.y;
					directionVector.z = neighborPos.z - nodePos.z;
				
					Commands[i] = new RaycastCommand(nodePosVector, directionVector, Query, directionVector.magnitude);
				}
			}
		}

		[BurstCompile]
		public struct FilterRaycastsJob : IJobParallelFor
		{
			[WriteOnly]
			public NativeArray<SIndexWithCost> Neighbors;

			[ReadOnly]
			public NativeArray<RaycastHit> Hits;

			public void Execute(int index)
			{
				if (Hits[index].colliderInstanceID == 0)
					return;

				Neighbors[index] = new SIndexWithCost();
			}
		}

		[BurstCompile]
		public struct FindPathJob : IJob
		{
			public NativeArray<SNode> Nodes;
			
			[ReadOnly]
			public NativeArray<SIndexWithCost> Neighbors;
			
			[WriteOnly]
			public NativeList<SNode> ResultingPath;

			public NativeHashSet<int> SearchedNodes;
			public NativeList<int> ToSearchNodes;
			
			public float Distance;
			
			public float3 StartPosition;
			public float3 EndPosition;
			
			public void Execute()
			{
				ResultingPath.Clear();
				SearchedNodes.Clear();
				ToSearchNodes.Clear();
				
				var distanceBetweenPoints = math.distance(StartPosition, EndPosition) / Distance;
				
				var startNodeIndex = findClosestNode(StartPosition);
				
				var startNode = Nodes[startNodeIndex];
				startNode.GCost = 0f;
				startNode.HCost = distanceBetweenPoints;
				startNode.FCost = distanceBetweenPoints;
				Nodes[startNodeIndex] = startNode;
				
				var endNodeIndex = findClosestNode(EndPosition);

				ToSearchNodes.Add(startNodeIndex);
				
				while (ToSearchNodes.Length > 0)
				{
					var nodeIndex = ToSearchNodes[0];
					var node = Nodes[nodeIndex];
					
					for (var i = 0; i < ToSearchNodes.Length; i++)
					{
						var searchingNodeIndex = ToSearchNodes[i];
						var searchingNode = Nodes[searchingNodeIndex];
						
						if (searchingNode.FCost < node.FCost || searchingNode.FCost == node.FCost && searchingNode.HCost < node.HCost)
							nodeIndex = searchingNodeIndex;
					}

					ToSearchNodes.RemoveAt(ToSearchNodes.IndexOf(nodeIndex));
					SearchedNodes.Add(nodeIndex);

					if (nodeIndex == endNodeIndex)
					{
						while (endNodeIndex != startNodeIndex)
						{
							var endNode = Nodes[endNodeIndex];
							
							ResultingPath.Add(endNode);
							endNodeIndex = endNode.Connection;
						}
			
						ResultingPath.Add(startNode);
						return;
					}
				
					calculateNeighbors(nodeIndex, EndPosition);
				}
			}

			private int findClosestNode(float3 worldPosition)
			{
				var closestDistance = Mathf.Infinity;
				var closestNode = -1;

				for (var i = 0; i < Nodes.Length; i++)
				{
					var dist = math.distancesq(Nodes[i].WorldPosition, worldPosition);
					if (dist > closestDistance)
						continue;
						
					closestDistance = dist;
					closestNode = i;
				}
			
				return closestNode;
			}
			
			private void calculateNeighbors(int nodeIndex, float3 endPosition)
			{
				var node = Nodes[nodeIndex];

				var startIndex = nodeIndex * 26;

				for (var i = startIndex; i < startIndex + 26; i++)
				{
					var neighbor = Neighbors[i];
					if (!neighbor.Valid)
						continue;

					var neighborIndex = neighbor.Index;
					
					var neighborNode = Nodes[neighborIndex];
					if (neighborNode.Availability != ENodeAvailabilityFlags.Available)
						continue;
				
					if (SearchedNodes.Contains(neighborIndex))
						continue;
					
					var gCost = node.GCost + neighbor.Cost;
					if (gCost >= neighborNode.GCost)
						continue;
					
					var neighborPos = neighborNode.WorldPosition;
					var hCost = math.distance(neighborPos, endPosition) / Distance;
					
					neighborNode.Connection = nodeIndex;
					neighborNode.GCost = gCost;
					neighborNode.HCost = hCost;
					neighborNode.FCost = gCost + hCost;
					Nodes[neighborIndex] = neighborNode;
				
					if (!ToSearchNodes.Contains(neighborIndex))
						ToSearchNodes.Add(neighborIndex);
				}
			}
		}
	}
}