using System.Diagnostics;
using AI.PathFinding.Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AI.PathFinding
{
	[ExecuteInEditMode]
	public class PathGridJobbified : MonoBehaviour
	{
		[Header("Grid Settings")]
		[SerializeField]
		public Vector3 Offset = Vector3.zero;
		[SerializeField]
		public Vector3 Size = Vector3.one;
		
		[SerializeField]
		public float Distance = 1f;

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
		[SerializeField]
		public Vector3 Start;
		[SerializeField]
		public Vector3 End;

		private NativeParallelMultiHashMap<int, int> connections;
		private NativeArray<SNode> nodes;
		
		private NativeHashSet<int> searchedNodes;
		private NativeHashSet<int> toSearchNodes;
		
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
			FindPath(Start, End);
			findStopwatch.Stop();
			Debug.Log($"Finding path [job] took {findStopwatch.ElapsedMilliseconds}ms");
		}

		public void OnDestroy()
		{
			if (connections.IsCreated)
				connections.Dispose();

			if (nodes.IsCreated)
				nodes.Dispose();
			
			if (searchedNodes.IsCreated)
				searchedNodes.Dispose();
			
			if (toSearchNodes.IsCreated)
				toSearchNodes.Dispose();
			
			if (resultingPath.IsCreated)
				resultingPath.Dispose();
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
							
					Gizmos.DrawSphere(node.WorldPosition, 0.1f);
				}
			}

			if (DrawConnections)
			{
				Gizmos.color = Color.green;

				foreach (var pair in connections)
				{
					var node = nodes[pair.Key];
					if ((node.Availability & DrawFlags) == 0)
						continue;

					var neighborNode = nodes[pair.Value];
					Gizmos.DrawLine(node.WorldPosition, neighborNode.WorldPosition);
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
		
		#endregion

		#region Path Grid

		public void CreateGrid()
		{
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			if (connections.IsCreated)
				connections.Dispose();

			if (nodes.IsCreated)
				nodes.Dispose();

			if (searchedNodes.IsCreated)
				searchedNodes.Dispose();
			
			if (toSearchNodes.IsCreated)
				toSearchNodes.Dispose();
			
			if (resultingPath.IsCreated)
				resultingPath.Dispose();
			
			connections = new NativeParallelMultiHashMap<int, int>(0, Allocator.Persistent);
			nodes = new NativeArray<SNode>(xSize * ySize * zSize, Allocator.Persistent);

			searchedNodes = new NativeHashSet<int>(0, Allocator.Persistent);
			toSearchNodes = new NativeHashSet<int>(0, Allocator.Persistent);

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
			
			#region Filter Inside Objects

			var bounds = new NativeList<Bounds>(Allocator.TempJob);

			var renderers = GetComponentsInChildren<Renderer>();
			for (var i = 0; i < renderers.Length; i++)
			{
				var rend = renderers[i];
				
				// Only check objects that are in the blocking mask
				if ((FilterMask.value & (1 << rend.gameObject.layer)) == 0)
					continue;
				
				bounds.Add(rend.bounds);
			}

			var findInsideObjectsJob = new FindInsideObjectsJob
			{
				Nodes = nodes,
				Bounds = bounds
			};

			var findInsideObjectsHandle = findInsideObjectsJob.Schedule(nodes.Length, BatchCount, initializeNodesHandle);
			
			#endregion

			#region Find Neighbor Connections

			var findNeighborConnectionsJob = new FindNeighborConnectionsJob
			{
				Nodes = nodes,
				Connections = connections,
				XSize = xSize,
				YSize = ySize,
				ZSize = zSize
			};

			var findNeighborConnectionsHandle = findNeighborConnectionsJob.Schedule(nodes.Length, findInsideObjectsHandle);
			findNeighborConnectionsHandle.Complete();

			#endregion

			#region Filter Raycast Neighbors

			var length = connections.Count();

			var raycastPairs = new NativeArray<KeyValue<int, int>>(length, Allocator.TempJob);
			var commands = new NativeArray<RaycastCommand>(length, Allocator.TempJob);
			var results = new NativeArray<RaycastHit>(length, Allocator.TempJob);

			var initializeRaycastsJob = new InitializeRaycastsJob
			{
				Nodes = nodes,
				Connections = connections,
				Commands = commands,
				Pairs = raycastPairs,
				FilterMask = FilterMask
			};

			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(findNeighborConnectionsHandle);
			
			var raycastHandle = RaycastCommand.ScheduleBatch(commands, results, BatchCount, 1, initializeRaycastsHandle);

			var filterConnectionsJob = new FilterConnectionsJob
			{
				Connections = connections,
				Pairs = raycastPairs,
				Hits = results
			};

			var filterConnectionsHandle = filterConnectionsJob.Schedule(results.Length, raycastHandle);
			filterConnectionsHandle.Complete();
			
			#endregion
			
			bounds.Dispose();
			raycastPairs.Dispose();
			commands.Dispose();
			results.Dispose();
		}

		public void FindPath(Vector3 start, Vector3 end)
		{
			var findPathJob = new FindPathJob
			{
				Nodes = nodes,
				Connections = connections,
				ResultingPath = resultingPath,
				SearchedNodes = searchedNodes,
				ToSearchNodes = toSearchNodes,
				Distance = Distance,
				StartPosition = start,
				EndPosition = end
			};

			var findPathHandle = findPathJob.Schedule();
			findPathHandle.Complete();
		}
		
		#endregion

		[BurstCompile]
		public struct SNode
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
		public struct FindInsideObjectsJob : IJobParallelFor
		{
			public NativeArray<SNode> Nodes;
			
			[ReadOnly]
			public NativeList<Bounds> Bounds;
			
			public void Execute(int index)
			{
				var node = Nodes[index];

				for (var i = 0; i < Bounds.Length; i++)
				{
					var bounds = Bounds[i];
					
					// If a node is inside bounds of an object, mark it unavailable
					if (!bounds.Contains(node.WorldPosition))
						continue;

					var availability = node.Availability;
							
					// Remove available flag
					availability &= ~ENodeAvailabilityFlags.Available;
							
					// Add inside object flag
					availability |= ENodeAvailabilityFlags.InsideObject;

					node.Availability = availability;
					
					Nodes[index] = node;
					
					// No point checking other bounds as it already is inside one
					break;
				}
			}
		}

		[BurstCompile]
		public struct FindNeighborConnectionsJob : IJobFor
		{
			public NativeArray<SNode> Nodes;

			public NativeParallelMultiHashMap<int, int> Connections;

			public int XSize;
			public int YSize;
			public int ZSize;
			
			public void Execute(int index)
			{
				var node = Nodes[index];
				
				// If a node does not have any connections, mark it unavailable
				if (findConnections(node))
					return;
				
				var availability = node.Availability;
				
				// Remove available flag
				availability &= ~ENodeAvailabilityFlags.Available;
				
				// Add no connections flag
				availability |= ENodeAvailabilityFlags.NoConnections;
				
				node.Availability = availability;
				
				Nodes[index] = node;
			}
			
			private bool findConnections(SNode node)
			{
				var any = false;
				var index = node.Index;
				var pos = node.GridPosition;
				
				var x = pos.x;
				var y = pos.y;
				var z = pos.z;

				var neighbors = new NativeHashSet<int>(0, Allocator.Temp);
				
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
							
							// Don't check connections to itself
							if (neighborIndex == index)
								continue;
							
							neighbors.Add(neighborIndex);
							any = true;
						}
					}
				}

				foreach (var neighbor in neighbors)
					Connections.Add(index, neighbor);
				
				neighbors.Dispose();
				
				return any;
			}
			
			private int getNodeIndex(int x, int y, int z)
			{
				return x * (ZSize * YSize) + y * ZSize + z;
			}
		}

		[BurstCompile]
		public struct InitializeRaycastsJob : IJob
		{
			[ReadOnly]
			public NativeArray<SNode> Nodes;
			
			[ReadOnly]
			public NativeParallelMultiHashMap<int, int> Connections;
			
			public NativeArray<RaycastCommand> Commands;

			[WriteOnly]
			public NativeArray<KeyValue<int, int>> Pairs;
			
			public LayerMask FilterMask;
			
			public void Execute()
			{
				var query = new QueryParameters(FilterMask);

				var index = 0;
				foreach (var pair in Connections)
				{
					var nodePos = Nodes[pair.Key].WorldPosition;
					var neighborPos = Nodes[pair.Value].WorldPosition;

					float3 direction;
					direction.x = neighborPos.x - nodePos.x;
					direction.y = neighborPos.y - nodePos.y;
					direction.z = neighborPos.z - nodePos.z;
				
					Commands[index] = new RaycastCommand(nodePos, direction, query, math.length(direction));
					Pairs[index] = pair;

					index++;
				}
			}
		}

		[BurstCompile]
		public struct FilterConnectionsJob : IJobFor
		{
			public NativeParallelMultiHashMap<int, int> Connections;

			[ReadOnly]
			public NativeArray<KeyValue<int, int>> Pairs;

			[ReadOnly]
			public NativeArray<RaycastHit> Hits;

			public void Execute(int index)
			{
				if (Hits[index].colliderInstanceID == 0)
					return;

				var pair = Pairs[index];
				Connections.Remove(pair.Key, pair.Value);
			}
		}

		[BurstCompile]
		public struct FindPathJob : IJob
		{
			public NativeArray<SNode> Nodes;
			
			[ReadOnly]
			public NativeParallelMultiHashMap<int, int> Connections;
			
			[WriteOnly]
			public NativeList<SNode> ResultingPath;

			public NativeHashSet<int> SearchedNodes;
			public NativeHashSet<int> ToSearchNodes;
			
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
				
				while (ToSearchNodes.Count > 0)
				{
					var nodeIndex = -1;

					// Find first node since hashset doesn't have indexer
					foreach (var searchingNodeIndex in ToSearchNodes)
					{
						nodeIndex = searchingNodeIndex;
						break;
					}

					var node = Nodes[nodeIndex];
					
					foreach (var searchingNodeIndex in ToSearchNodes)
					{
						var searchingNode = Nodes[searchingNodeIndex];
						
						if (searchingNode.FCost < node.FCost || searchingNode.FCost == node.FCost && searchingNode.HCost < node.HCost)
							nodeIndex = searchingNodeIndex;
					}

					ToSearchNodes.Remove(nodeIndex);
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

				var values = Connections.GetValuesForKey(nodeIndex);
				foreach (var neighborNodeIndex in values)
				{
					var neighborNode = Nodes[neighborNodeIndex];
					if (neighborNode.Availability != ENodeAvailabilityFlags.Available)
						continue;
				
					if (SearchedNodes.Contains(neighborNodeIndex))
						continue;

					var gCost = node.GCost + math.distance(node.WorldPosition, neighborNode.WorldPosition) / Distance;
					if (gCost >= neighborNode.GCost)
						continue;
					
					var hCost = math.distance(neighborNode.WorldPosition, endPosition) / Distance;
					
					neighborNode.Connection = nodeIndex;
					neighborNode.GCost = gCost;
					neighborNode.HCost = hCost;
					neighborNode.FCost = gCost + hCost;
					Nodes[neighborNodeIndex] = neighborNode;
				
					ToSearchNodes.Add(neighborNodeIndex);
				}
			}
		}
	}
}