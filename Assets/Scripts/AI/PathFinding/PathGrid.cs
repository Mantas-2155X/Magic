using AI.PathFinding.Jobs;
using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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
		
		[Header("Path Finding")]
		[SerializeField][Range(0.5f, 1f)]
		public float Accuracy = 0.75f;
		[SerializeField]
		public Vector3 Start;
		[SerializeField]
		public Vector3 End;

		public bool createGridRequested;
		public bool findPathRequested;
		
		public EPathFindingStatus Status { get; private set; } = EPathFindingStatus.Idle;

		#region Internals

		private NativeArray<SNode> nodes;
		private NativeArray<SIndexWithCost> neighbors;

		private NativeArray<OverlapSphereCommand> overlapCommands;
		private NativeArray<ColliderHit> overlapResults;

		private NativeArray<RaycastCommand> raycastCommands;
		private NativeArray<RaycastHit> raycastResults;
		
		private NativeHashSet<int> searchedNodes;
		private NativeList<int> toSearchNodes;
		
		private NativeList<SNode> resultingPath;

		private JobHandle filterRaycastsHandle;
		private JobHandle findPathHandle;
		
		private int xSize;
		private int ySize;
		private int zSize;
		
		private double statusChangedTime;
		
		#endregion

		#region MonoBehaviour

		public void Update()
		{
			if (Status == EPathFindingStatus.Idle)
			{
				if (createGridRequested)
				{
					Status = EPathFindingStatus.CreatingGrid;
					statusChangedTime = Time.time;
					//createGridRequested = false;
					
					CreateGrid();
				}
				else if (findPathRequested)
				{
					Status = EPathFindingStatus.FindingPath;
					statusChangedTime = Time.time;
					//findPathRequested = false;
					
					FindPath();
				}
			}
			else if (Status == EPathFindingStatus.CreatingGrid && filterRaycastsHandle.IsCompleted)
			{
				filterRaycastsHandle.Complete();
				Debug.Log($"Created grid [job] (nodes {xSize * ySize * zSize} neighbors {neighbors.Length}) took {Time.time - statusChangedTime} s");
				
				Status = EPathFindingStatus.Idle;
				statusChangedTime = Time.time;

				if (findPathRequested)
				{
					Status = EPathFindingStatus.FindingPath;
					statusChangedTime = Time.time;
					//findPathRequested = false;
					
					FindPath();
				}
			}
			else if (Status == EPathFindingStatus.FindingPath && findPathHandle.IsCompleted)
			{
				findPathHandle.Complete();
				Debug.Log($"Found path [job] (searched {searchedNodes.Count} result {resultingPath.Length}) took {Time.time - statusChangedTime} s");
				
				Status = EPathFindingStatus.Idle;
				statusChangedTime = Time.time;
			}
		}

		public void OnDestroy()
		{
			cleanup();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (Status != EPathFindingStatus.Idle || !nodes.IsCreated)
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

			if (overlapCommands.IsCreated)
				overlapCommands.Dispose();
			
			if (overlapResults.IsCreated)
				overlapResults.Dispose();
			
			if (raycastCommands.IsCreated)
				raycastCommands.Dispose();
			
			if (raycastResults.IsCreated)
				raycastResults.Dispose();
			
			if (searchedNodes.IsCreated)
				searchedNodes.Dispose();
			
			if (toSearchNodes.IsCreated)
				toSearchNodes.Dispose();
			
			if (resultingPath.IsCreated)
				resultingPath.Dispose();
		}
		
		#endregion

		#region Path Grid

		public void CreateGrid()
		{
			cleanup();
			
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			var nodesLength = xSize * ySize * zSize;
			var neighborsLength = 26 * nodesLength;
			var batchCount = nodesLength / (JobsUtility.JobWorkerCount - 2);

			nodes = new NativeArray<SNode>(nodesLength, Allocator.Persistent);
			neighbors = new NativeArray<SIndexWithCost>(neighborsLength, Allocator.Persistent);

			overlapCommands = new NativeArray<OverlapSphereCommand>(nodesLength, Allocator.Persistent);
			overlapResults = new NativeArray<ColliderHit>(nodesLength, Allocator.Persistent);

			raycastCommands = new NativeArray<RaycastCommand>(neighborsLength, Allocator.Persistent);
			raycastResults = new NativeArray<RaycastHit>(neighborsLength, Allocator.Persistent);

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
			
			var initializeOverlapsJob = new InitializeOverlapsJob
			{
				Nodes = nodes,
				Commands = overlapCommands,
				Radius = Radius,
				Query = new QueryParameters(FilterMask)
			};

			var initializeOverlapsHandle = initializeOverlapsJob.Schedule(nodesLength, batchCount, initializeNodesHandle);
			
			var overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapResults, batchCount, 1, initializeOverlapsHandle);

			var filterOverlapsJob = new FilterOverlapsJob
			{
				Nodes = nodes,
				Hits = overlapResults
			};

			var filterOverlapsHandle = filterOverlapsJob.Schedule(overlapResults.Length, batchCount, overlapHandle);
			
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

			var initializeNeighborsHandle = initializeNeighborsJob.Schedule(nodesLength, batchCount, filterOverlapsHandle);

			#endregion

			#region Filter Raycasts

			var initializeRaycastsJob = new InitializeRaycastsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Commands = raycastCommands,
				Query = new QueryParameters(FilterMask, hitBackfaces: true)
			};

			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(nodesLength, batchCount, initializeNeighborsHandle);
			
			var raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, batchCount, 1, initializeRaycastsHandle);

			var filterRaycastsJob = new FilterRaycastsJob
			{
				Neighbors = neighbors,
				Hits = raycastResults
			};

			filterRaycastsHandle = filterRaycastsJob.Schedule(raycastResults.Length, batchCount, raycastHandle);
			
			#endregion
		}

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

			findPathHandle = findPathJob.Schedule();
		}
		
		#endregion
	}
}