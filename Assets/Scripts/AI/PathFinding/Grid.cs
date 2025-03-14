using System;
using AI.PathFinding.Jobs;
using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Events;

namespace AI.PathFinding
{
	public class Grid : MonoBehaviour
	{
		[Header("Grid Settings")]
		[SerializeField]
		public Vector3 Offset = Vector3.zero;
		[SerializeField]
		public Vector3 Size = Vector3.one;
		
		[SerializeField][Range(0.1f, 10f)]
		public float Distance = 1f;

		[SerializeField][Range(0.01f, 5f)]
		public float Radius = 0.25f;

		[Header("Filter Settings")]
		[SerializeField]
		public LayerMask FilterMask = -1;

		[Header("Draw Types")]
		[SerializeField]
		public bool DrawBounds;
		[SerializeField]
		public bool DrawNodes;
		[SerializeField]
		public bool DrawConnections;
		
		[Header("Draw Flags")]
		[SerializeField]
		public bool DrawAvailable;
		[SerializeField]
		public bool DrawInsideObject;
		[SerializeField]
		public bool DrawNoConnections;
		
		[Header("Path Finding")]
		[SerializeField][Range(0.5f, 1f)]
		public float Accuracy = 0.85f;
		
		public int NodesLength { get; private set; }
		public int NeighborsLength { get; private set; }

		public int NodesBatchCount => NodesLength / (JobsUtility.JobWorkerCount / 2);
		public int NeighborsBatchCount => NeighborsLength / (JobsUtility.JobWorkerCount / 2);
		
		public EGridStatus Status { get; private set; } = EGridStatus.NotInitialized;

		private NativeArray<SNode> nodes;
		private NativeArray<SIndexWithCost> neighbors;

		private NativeArray<ENodeAvailability> availabilities;
		
		private NativeArray<OverlapSphereCommand> overlapCommands;
		private NativeArray<ColliderHit> overlapResults;

		private NativeArray<RaycastCommand> raycastCommands;
		private NativeArray<RaycastHit> raycastResults;
		
		private int xSize;
		private int ySize;
		private int zSize;
		
		#region MonoBehaviour

		public void OnDestroy()
		{
			cleanup();
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (DrawBounds)
				Gizmos.DrawWireCube(transform.position + Offset, Size);

			if (DrawNodes && Status == EGridStatus.Initialized && nodes.IsCreated)
			{
				for (var i = 0; i < nodes.Length; i++)
				{
					var availability = availabilities[i];
					switch (availability)
					{
						case ENodeAvailability.Available:
						{
							if (!DrawAvailable)
								continue;
							
							Gizmos.color = Color.green;
							break;
						}
						case ENodeAvailability.InsideObject:
						{
							if (!DrawInsideObject)
								continue;
						
							Gizmos.color = Color.red;
							break;
						}
						case ENodeAvailability.NoConnections:
						{
							if (!DrawNoConnections)
								continue;
						
							Gizmos.color = Color.yellow;
							break;
						}
						default:
							throw new NotImplementedException();
					}
							
					Gizmos.DrawSphere(nodes[i].WorldPosition, Radius);
				}
			}

			if (DrawConnections && Status == EGridStatus.Initialized && nodes.IsCreated)
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
		}
#endif
		
		#endregion

		#region API

		/// <summary>
		/// Creates the pathfinding grid from ground up
		/// Returns true if the grid creation was successful
		/// </summary>
		public async UniTask<bool> CreateGrid(UnityAction<bool> callback = null)
		{
			// Can only create a grid if one isn't already being made
			if (Status == EGridStatus.Initializing)
			{
				if (callback != null)
					callback.Invoke(false);
				
				return false;
			}

			Status = EGridStatus.Initializing;
			
			// Set up grid size in amount of nodes
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			var nodesLength = xSize * ySize * zSize;
			var neighborsLength = 26 * nodesLength;

			// Amount of nodes changed, dispose of old data and create it from ground up
			if (nodesLength != NodesLength || neighborsLength != NeighborsLength)
			{
				cleanup();
				
				nodes = new NativeArray<SNode>(nodesLength, Allocator.Persistent);
				neighbors = new NativeArray<SIndexWithCost>(neighborsLength, Allocator.Persistent);

				availabilities = new NativeArray<ENodeAvailability>(nodesLength, Allocator.Persistent);
				
				overlapCommands = new NativeArray<OverlapSphereCommand>(nodesLength, Allocator.Persistent);
				overlapResults = new NativeArray<ColliderHit>(nodesLength, Allocator.Persistent);

				raycastCommands = new NativeArray<RaycastCommand>(neighborsLength, Allocator.Persistent);
				raycastResults = new NativeArray<RaycastHit>(neighborsLength, Allocator.Persistent);
				
				NodesLength = nodesLength;
				NeighborsLength = neighborsLength;
			}
			
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

			// Wait so we have the data ready for the physics job as it might lock up main thread
			var initializeOverlapsHandle = initializeOverlapsJob.Schedule(NodesLength, NodesBatchCount, initializeNodesHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => initializeOverlapsHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			initializeOverlapsHandle.Complete();
			
			// Wait for physics job as it might lock up main thread
			var overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapResults, NodesBatchCount, 1, initializeOverlapsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => overlapHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			overlapHandle.Complete();

			var filterOverlapsJob = new FilterOverlapsJob
			{
				Availabilities = availabilities,
				Hits = overlapResults
			};

			var filterOverlapsHandle = filterOverlapsJob.Schedule(NodesLength, NodesBatchCount, overlapHandle);
			
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

			var initializeNeighborsHandle = initializeNeighborsJob.Schedule(NodesLength, NodesBatchCount, filterOverlapsHandle);

			#endregion

			#region Filter Raycasts

			var initializeRaycastsJob = new InitializeRaycastsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Commands = raycastCommands,
				Query = new QueryParameters(FilterMask, hitBackfaces: true)
			};

			// Wait so we have the data ready for the physics job as it might lock up main thread
			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(NeighborsLength, NeighborsBatchCount, initializeNeighborsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => initializeRaycastsHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			initializeRaycastsHandle.Complete();
			
			// Wait for physics job as it might lock up main thread
			var raycastHandle = RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, NeighborsBatchCount, 1, initializeRaycastsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => raycastHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			raycastHandle.Complete();

			var filterRaycastsJob = new FilterRaycastsJob
			{
				Neighbors = neighbors,
				Hits = raycastResults.Slice().SliceConvert<SRaycastHit>(),
				Empty = new SIndexWithCost()
			};

			var filterRaycastsHandle = filterRaycastsJob.Schedule(NeighborsLength, NeighborsBatchCount, raycastHandle);
			await UniTask.WaitUntil(() => filterRaycastsHandle.IsCompleted);
			filterRaycastsHandle.Complete();
			
			#endregion
			
			Status = EGridStatus.Initialized;
			
			if (callback != null)
				callback.Invoke(true);

			return true;
		}

		/// <summary>
		/// Finds a path between two given vectors
		/// Returns an array of nodes that follow the path
		/// </summary>
		public async UniTask<Path> FindPath(Vector3 startPosition, Vector3 endPosition, UnityAction<Path> callback = null)
		{
			// Can only find a path if the grid is created
			if (Status != EGridStatus.Initialized)
			{
				if (callback != null)
					callback.Invoke(null);

				return null;
			}
			
			var nodesLength = xSize * ySize * zSize;
			var nodesBatchCount = nodesLength / (JobsUtility.JobWorkerCount / 2);

			#region Clear Path

			var gCosts = new NativeArray<float>(nodesLength, Allocator.Persistent);
			var hCosts = new NativeArray<float>(nodesLength, Allocator.Persistent);
			var fCosts = new NativeArray<float>(nodesLength, Allocator.Persistent);
			var connections = new NativeArray<int>(nodesLength, Allocator.Persistent);
			
			var initializePathJob = new InitializePathJob
			{
				GCosts = gCosts,
				HCosts = hCosts,
				FCosts = fCosts,
				Connections = connections
			};

			var initializePathHandle = initializePathJob.Schedule(nodesLength, nodesBatchCount);

			#endregion

			#region Find Path

			var searchedNodes = new NativeHashSet<int>(nodesLength, Allocator.Persistent);
			var toSearchNodes = new NativeList<int>(nodesLength, Allocator.Persistent);
			var resultingPath = new NativeList<int>(Allocator.Persistent);
			
			var findPathJob = new FindPathJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Availabilities = availabilities,
				GCosts = gCosts,
				HCosts = hCosts,
				FCosts = fCosts,
				Connections = connections,
				ResultingPath = resultingPath,
				SearchedNodes = searchedNodes,
				ToSearchNodes = toSearchNodes,
				Distance = Distance,
				StartPosition = startPosition,
				EndPosition = endPosition
			};

			var findPathHandle = findPathJob.Schedule(initializePathHandle);
			await UniTask.WaitUntil(() => findPathHandle.IsCompleted);
			findPathHandle.Complete();

			Path result = null;
			
			if (resultingPath.Length != 0)
			{
				var points = new Vector3[resultingPath.Length];
				
				for (var i = 0; i < resultingPath.Length; i++)
					points[i] = nodes[resultingPath[i]].WorldPosition;
				
				result = new Path(points, searchedNodes.Count);
			}
			
			#endregion

			gCosts.Dispose();
			hCosts.Dispose();
			fCosts.Dispose();
			connections.Dispose();

			searchedNodes.Dispose();
			toSearchNodes.Dispose();
			resultingPath.Dispose();
			
			if (callback != null)
				callback.Invoke(result);

			return result;
		}
		
		#endregion

		#region Internals

		private void cleanup()
		{
			if (nodes.IsCreated)
				nodes.Dispose();
			
			if (neighbors.IsCreated)
				neighbors.Dispose();

			if (availabilities.IsCreated)
				availabilities.Dispose();

			if (overlapCommands.IsCreated)
				overlapCommands.Dispose();
			
			if (overlapResults.IsCreated)
				overlapResults.Dispose();
			
			if (raycastCommands.IsCreated)
				raycastCommands.Dispose();
			
			if (raycastResults.IsCreated)
				raycastResults.Dispose();
		}

		#endregion
	}
}