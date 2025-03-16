#define DEBUG_TIMINGS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using AI.PathFinding.Jobs;
using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Cysharp.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace AI.PathFinding
{
	[ExecuteInEditMode]
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
		[SerializeField]
		public bool DrawPaths;
		
		[Header("Draw Flags")]
		[SerializeField]
		public bool DrawAvailable;
		[SerializeField]
		public bool DrawInsideObject;
		[SerializeField]
		public bool DrawObstructed;
		[SerializeField]
		public bool DrawSearched;
		
		[Header("Path Finding")]
		[SerializeField][Range(0.5f, 1f)]
		public float Accuracy = 0.9f;

		[SerializeField][Range(0.1f, 2.5f)]
		public float UpdateObstaclesEvery = 0.5f;
		
		public int NodesLength { get; private set; }
		public int NeighborsLength { get; private set; }

		public int DelayedPathFinds { get; private set; }
		public int WaitingPathFinds { get; private set; }
		public int ActivePathFinds { get; private set; }

		public EGridStatus Status { get; private set; } = EGridStatus.NotInitialized;

		public int MaximumWorkers => JobsUtility.JobWorkerCount / 2;

		public static readonly List<Grid> Grids = new ();

		private NativeArray<SNode> nodes;
		private NativeArray<SIndexWithCost> neighbors;

		private NativeArray<ENodeAvailabilityFlags> availabilities;
		
		private NativeArray<OverlapSphereCommand> overlapCommands;
		private NativeArray<ColliderHit> overlapResults;

		private NativeArray<SpherecastCommand> raycastCommands;
		private NativeArray<RaycastHit> raycastResults;
		
		private int xSize;
		private int ySize;
		private int zSize;

		private bool updatingObstacles;
		private float lastObstacleUpdate;
		
		#region MonoBehaviour

		public void Start()
		{
			CreateGrid().Forget();
		}

		public void Update()
		{
			if (updatingObstacles)
				return;
			
			if (Status != EGridStatus.Initialized || !nodes.IsCreated)
				return;
			
			var time = Time.time;
			if (time < lastObstacleUpdate + UpdateObstaclesEvery)
				return;
			
			updateObstacles();
		}
		
		public void OnEnable()
		{
			Grids.Add(this);
		}

		public void OnDisable()
		{
			Grids.Remove(this);
		}

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
					if (availability == ENodeAvailabilityFlags.Available)
					{
						if (!DrawAvailable)
							continue;

						Gizmos.color = Color.green;
					}
					else if (availability.HasFlag(ENodeAvailabilityFlags.InsideObject))
					{
						if (!DrawInsideObject)
							continue;

						Gizmos.color = Color.red;
					}
					else if (availability.HasFlag(ENodeAvailabilityFlags.Obstructed))
					{
						if (!DrawObstructed)
							continue;

						Gizmos.color = Color.black;
					}
					else
					{
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
						if (!neighbor.Connects)
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
				Debug.LogWarning("[Grid] Skipping grid creation as it is already being created");
				
				if (callback != null)
					callback.Invoke(false);
				
				return false;
			}

			Status = EGridStatus.Initializing;
			
			// Wait for path requests to finish before recreating grid
			if (ActivePathFinds != 0 || WaitingPathFinds != 0)
			{
				Debug.LogWarning("[Grid] Waiting grid creation until all path requests are done");
				await UniTask.WaitUntil(() => ActivePathFinds == 0 && WaitingPathFinds == 0);
			}

			// Wait for obstacle update to finish before recreating grid
			if (updatingObstacles)
			{
				Debug.LogWarning("[Grid] Waiting grid creation until obstacles are updated");
				await UniTask.WaitUntil(() => !updatingObstacles);
			}
			
#if DEBUG_TIMINGS
			var totalWatch = new Stopwatch();
			totalWatch.Start();
#endif

			// Set up grid size in amount of nodes
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			var nodesLength = xSize * ySize * zSize;
			var neighborsLength = 26 * nodesLength;

			// Amount of nodes changed, dispose of old data and create it from ground up
			if (nodesLength != NodesLength || neighborsLength != NeighborsLength || !nodes.IsCreated)
			{
				cleanup();
				
				nodes = new NativeArray<SNode>(nodesLength, Allocator.Persistent);
				neighbors = new NativeArray<SIndexWithCost>(neighborsLength, Allocator.Persistent);

				availabilities = new NativeArray<ENodeAvailabilityFlags>(nodesLength, Allocator.Persistent);
				
				overlapCommands = new NativeArray<OverlapSphereCommand>(nodesLength, Allocator.Persistent);
				overlapResults = new NativeArray<ColliderHit>(nodesLength, Allocator.Persistent);

				raycastCommands = new NativeArray<SpherecastCommand>(neighborsLength, Allocator.Persistent);
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

#if DEBUG_TIMINGS
			var watch = new Stopwatch();
			watch.Start();
			var initializeNodesHandle = initializeNodesJob.Schedule();
			initializeNodesHandle.Complete();
			watch.Stop();
			Debug.Log($"initializeNodesJob took {watch.ElapsedMilliseconds}ms");
#else
			var initializeNodesHandle = initializeNodesJob.Schedule();
#endif

			#endregion

			#region Initialize Areas

			var areasList = Area.Areas;
			var areasLength = areasList.Count;

			var positions = new NativeArray<float3>(areasLength, Allocator.Persistent);
			var halfSizes = new NativeArray<float3>(areasLength, Allocator.Persistent);
			var areaCosts = new NativeArray<float>(areasLength, Allocator.Persistent);
			
			for (var i = 0; i < areasLength; i++)
			{
				var area = areasList[i];
				
				positions[i] = area.GetPosition();
				halfSizes[i] = area.GetHalfSize();
				areaCosts[i] = area.Cost;
			}

			var initializeAreasJob = new InitializeAreasJob
			{
				Nodes = nodes,
				Positions = positions,
				HalfSizes = halfSizes,
				AreaCosts = areaCosts,
				HalfRadius = Radius / 2f
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var initializeAreasHandle = initializeAreasJob.Schedule(nodesLength, nodesLength / MaximumWorkers, initializeNodesHandle);
			initializeAreasHandle.Complete();
			watch.Stop();
			Debug.Log($"initializeAreasHandle took {watch.ElapsedMilliseconds}ms");
#else
			var initializeAreasHandle = initializeAreasJob.Schedule(nodesLength, nodesLength / MaximumWorkers, initializeNodesHandle);
#endif

			#endregion
			
			#region Filter Overlaps
			
			var initializeOverlapsJob = new InitializeOverlapsJob
			{
				Nodes = nodes,
				Commands = overlapCommands,
				Radius = Radius,
				Query = new QueryParameters(FilterMask)
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var initializeOverlapsHandle = initializeOverlapsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, initializeAreasHandle);
			initializeOverlapsHandle.Complete();
			watch.Stop();
			Debug.Log($"initializeOverlapsHandle took {watch.ElapsedMilliseconds}ms");
#else
			// Wait so we have the data ready for the physics job as it might lock up main thread
			var initializeOverlapsHandle = initializeOverlapsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, initializeAreasHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => initializeOverlapsHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			initializeOverlapsHandle.Complete();
#endif
			
#if DEBUG_TIMINGS
			watch.Restart();
			var overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapResults, nodesLength / MaximumWorkers, 1, initializeOverlapsHandle);
			overlapHandle.Complete();
			watch.Stop();
			Debug.Log($"overlapHandle took {watch.ElapsedMilliseconds}ms");
#else
			// Wait for physics job as it might lock up main thread
			var overlapHandle = OverlapSphereCommand.ScheduleBatch(overlapCommands, overlapResults, nodesLength / MaximumWorkers, 1, initializeOverlapsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => overlapHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			overlapHandle.Complete();
#endif

			var filterOverlapsJob = new FilterOverlapsJob
			{
				Availabilities = availabilities,
				Hits = overlapResults
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var filterOverlapsHandle = filterOverlapsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, overlapHandle);
			filterOverlapsHandle.Complete();
			watch.Stop();
			Debug.Log($"filterOverlapsHandle took {watch.ElapsedMilliseconds}ms");
#else
			var filterOverlapsHandle = filterOverlapsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, overlapHandle);
#endif
			
			#endregion

			#region Initialize Neighbors

			var initializeNeighborsJob = new InitializeNeighborsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Accuracy = Accuracy,
				XSize = xSize,
				YSize = ySize,
				ZSize = zSize
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var initializeNeighborsHandle = initializeNeighborsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, filterOverlapsHandle);
			initializeNeighborsHandle.Complete();
			watch.Stop();
			Debug.Log($"initializeNeighborsHandle took {watch.ElapsedMilliseconds}ms");
#else
			var initializeNeighborsHandle = initializeNeighborsJob.Schedule(NodesLength, nodesLength / MaximumWorkers, filterOverlapsHandle);
#endif

			#endregion

			#region Filter Raycasts

			var initializeRaycastsJob = new InitializeRaycastsJob
			{
				Nodes = nodes,
				Neighbors = neighbors,
				Commands = raycastCommands,
				Radius = Radius,
				Query = new QueryParameters(FilterMask, hitBackfaces: true)
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(NeighborsLength, neighborsLength / MaximumWorkers, initializeNeighborsHandle);
			initializeRaycastsHandle.Complete();
			watch.Stop();
			Debug.Log($"initializeRaycastsHandle took {watch.ElapsedMilliseconds}ms");
#else
			// Wait so we have the data ready for the physics job as it might lock up main thread
			var initializeRaycastsHandle = initializeRaycastsJob.Schedule(NeighborsLength, neighborsLength / MaximumWorkers, initializeNeighborsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => initializeRaycastsHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			initializeRaycastsHandle.Complete();
#endif
			
#if DEBUG_TIMINGS
			watch.Restart();
			var raycastHandle = SpherecastCommand.ScheduleBatch(raycastCommands, raycastResults, neighborsLength / MaximumWorkers, 1, initializeRaycastsHandle);
			raycastHandle.Complete();
			watch.Stop();
			Debug.Log($"raycastHandle took {watch.ElapsedMilliseconds}ms");
#else
			// Wait for physics job as it might lock up main thread
			var raycastHandle = SpherecastCommand.ScheduleBatch(raycastCommands, raycastResults, neighborsLength / MaximumWorkers, 1, initializeRaycastsHandle);
			await UniTask.WaitForFixedUpdate();
			await UniTask.WaitUntil(() => raycastHandle.IsCompleted);
			await UniTask.WaitForFixedUpdate();
			raycastHandle.Complete();
#endif

			var filterRaycastsJob = new FilterRaycastsJob
			{
				Neighbors = neighbors,
				Hits = raycastResults,
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var filterRaycastsHandle = filterRaycastsJob.Schedule(NeighborsLength, neighborsLength / MaximumWorkers, raycastHandle);
			filterRaycastsHandle.Complete();
			watch.Stop();
			Debug.Log($"filterRaycastsHandle took {watch.ElapsedMilliseconds}ms");
#else
			var filterRaycastsHandle = filterRaycastsJob.Schedule(NeighborsLength, neighborsLength / MaximumWorkers, raycastHandle);
			await UniTask.WaitUntil(() => filterRaycastsHandle.IsCompleted);
			filterRaycastsHandle.Complete();
#endif
			
			#endregion
			
			positions.Dispose();
			halfSizes.Dispose();
			areaCosts.Dispose();
			
			Status = EGridStatus.Initialized;
			
#if DEBUG_TIMINGS
			totalWatch.Stop();
			Debug.Log($"CreateGrid total took {totalWatch.ElapsedMilliseconds}ms");
#endif
			
			if (callback != null)
				callback.Invoke(true);
			
			return true;
		}

		/// <summary>
		/// Finds a path between two given vectors
		/// Returns an array of nodes that follow the path
		/// </summary>
		public async UniTask<Path> FindPath(Vector3 startPosition, Vector3 endPosition, int identifier = 0, UnityAction<Path> callback = null)
		{
			// Can only find a path if the grid is created
			if (Status == EGridStatus.NotInitialized)
			{
				Debug.LogWarning("[Grid] Ignoring path request because grid is not created");

				if (callback != null)
					callback.Invoke(null);

				return null;
			}
			
			// If a grid is being created, wait for that to finish
			if (Status == EGridStatus.Initializing)
			{
				DelayedPathFinds++;
				
				Debug.LogWarning("[Grid] Delaying path request until grid finishes creating");
				await UniTask.WaitUntil(() => Status == EGridStatus.Initialized);

				DelayedPathFinds--;
			}

			// Don't create too many path requests at the same time, wait for some to finish
			if (ActivePathFinds >= MaximumWorkers)
			{
				WaitingPathFinds++;

				Debug.LogWarning("[Grid] Waiting path request until there is more queue space");
				await UniTask.WaitUntil(() => ActivePathFinds < MaximumWorkers);
				
				WaitingPathFinds--;
			}

			ActivePathFinds++;
			
#if DEBUG_TIMINGS
			var totalWatch = new Stopwatch();
			totalWatch.Start();
#endif
			
			var nodesRadius = Radius;
			var nodesLength = xSize * ySize * zSize;
			var nodesBatchCount = nodesLength / (JobsUtility.JobWorkerCount / 2);
			
			#region Initialize Path

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

#if DEBUG_TIMINGS
			var watch = new Stopwatch();
			watch.Start();
			var initializePathHandle = initializePathJob.Schedule(nodesLength, nodesBatchCount);
			initializePathHandle.Complete();
			watch.Stop();
			Debug.Log($"initializePathHandle took {watch.ElapsedMilliseconds}ms");
#else
			var initializePathHandle = initializePathJob.Schedule(nodesLength, nodesBatchCount);
#endif

			#endregion

			#region Find Path

			var searchedNodes = new NativeHashSet<int>(nodesLength, Allocator.Persistent);
			var toSearchNodes = new NativeList<int>(nodesLength, Allocator.Persistent);
			var resultingPath = new NativeList<int>(Allocator.Persistent);
			
			var availabilitiesCopy = new NativeArray<ENodeAvailabilityFlags>(nodesLength, Allocator.Persistent);
			await UniTask.WaitUntil(() => !updatingObstacles);
			availabilitiesCopy.CopyFrom(availabilities);

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
				StartPosition = startPosition,
				EndPosition = endPosition
			};

#if DEBUG_TIMINGS
			watch.Restart();
			var findPathHandle = findPathJob.Schedule(initializePathHandle);
			findPathHandle.Complete();
			watch.Stop();
			Debug.Log($"findPathHandle took {watch.ElapsedMilliseconds}ms");
#else
			var findPathHandle = findPathJob.Schedule(initializePathHandle);
			await UniTask.WaitUntil(() => findPathHandle.IsCompleted);
			findPathHandle.Complete();
#endif

			var result = Path.Create(nodes, searchedNodes, resultingPath, nodesRadius, identifier);
			
			#endregion
			
			gCosts.Dispose();
			hCosts.Dispose();
			fCosts.Dispose();
			connections.Dispose();

			searchedNodes.Dispose();
			toSearchNodes.Dispose();
			resultingPath.Dispose();

			availabilitiesCopy.Dispose();
			
#if DEBUG_TIMINGS
			totalWatch.Stop();
			Debug.Log($"FindPath total took {totalWatch.ElapsedMilliseconds}ms");
#endif
			
			if (callback != null)
				callback.Invoke(result);

			ActivePathFinds--;

			return result;
		}
		
		#endregion

		#region Internals

		private void updateObstacles()
		{
			updatingObstacles = true;
			
			var obstacles = Obstacle.Obstacles;
			var obstaclesLength = obstacles.Count;

			var positions = new NativeArray<float3>(obstaclesLength, Allocator.TempJob);
			var halfSizes = new NativeArray<float3>(obstaclesLength, Allocator.TempJob);
			
			var nodesLength = NodesLength;
			var nodesBatchCount = nodesLength / (JobsUtility.JobWorkerCount / 2);

			for (var i = 0; i < obstaclesLength; i++)
			{
				var obstacle = obstacles[i];
				
				positions[i] = obstacle.GetPosition();
				halfSizes[i] = obstacle.GetHalfSize();
			}

			var filterObstaclesJob = new FilterObstaclesJob
			{
				Nodes = nodes,
				Positions = positions,
				HalfSizes = halfSizes,
				Availabilities = availabilities,
				HalfRadius = Radius / 2f
			};

#if DEBUG_TIMINGS
			var watch = new Stopwatch();
			watch.Start();
			var filterObstaclesHandle = filterObstaclesJob.Schedule(nodesLength, nodesBatchCount);
			filterObstaclesHandle.Complete();
			watch.Stop();
			Debug.Log($"filterObstaclesHandle took {watch.ElapsedMilliseconds}ms");
#else
			var filterObstaclesHandle = filterObstaclesJob.Schedule(nodesLength, nodesBatchCount);
			filterObstaclesHandle.Complete();
#endif
			
			positions.Dispose();
			halfSizes.Dispose();

#if DEBUG_TIMINGS
			watch.Restart();
#endif
			
			var agents = Agent.Agents;
			var agentsLength = agents.Count;

			for (var i = 0; i < NodesLength; i++)
			{
				var availability = availabilities[i];
				if ((availability & ENodeAvailabilityFlags.Obstructed) == 0)
					continue;
				
				for (var k = 0; k < agentsLength; k++)
				{
					var agent = agents[k];
					if (agent == null || !agent.HasPath)
						continue;
					
					if (Array.IndexOf(agent.Path.Indexes, i) < agent.BeforeSkipNextNodeIndex)
						continue;

					Debug.Log($"[Grid] Recalculating path for {agent.name} as it is obstructed");
					agent.SetDestination(agent.LastNode);
					break;
				}
			}
			
#if DEBUG_TIMINGS
			watch.Stop();
			Debug.Log($"obstruction recalculation check took {watch.ElapsedMilliseconds}ms");
#endif
			
			lastObstacleUpdate = Time.time;
			updatingObstacles = false;
		}
		
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