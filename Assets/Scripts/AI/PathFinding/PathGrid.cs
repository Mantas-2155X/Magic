using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		
		[SerializeField]
		public float Distance = 1f;

		[Header("Filter Settings")]
		[SerializeField]
		public bool FilterInsideObjects = true;
		
		[SerializeField]
		public LayerMask FilterMask = -1;

		[Header("Draw Settings")]
		public bool DrawBounds = true;
		
		public bool DrawNodes = true;

		public bool DrawConnections = true;

		public bool DrawPath = true;
		
		public ENodeAvailability DrawFlags = (ENodeAvailability)~0;
		
		private Node[][][] nodes;
		
		private int xSize;
		private int ySize;
		private int zSize;

		private readonly HashSet<Node> searchedNodes = new ();
		private readonly List<Node> toSearchNodes = new ();
		private readonly List<Node> resultingPath = new ();

		public Vector3 Start;
		public Vector3 End;
		
		#region MonoBehaviour

		public void Update()
		{
			var createStopwatch = new Stopwatch();
			createStopwatch.Start();
			CreateGrid();
			createStopwatch.Stop();
			Debug.Log($"Creating grid took {createStopwatch.ElapsedMilliseconds}ms");

			var findStopwatch = new Stopwatch();
			findStopwatch.Start();
			FindPath(Start, End);
			findStopwatch.Stop();
			Debug.Log($"Finding path took {findStopwatch.ElapsedMilliseconds}ms");
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (nodes == null)
				return;
			
			if (DrawBounds)
				Gizmos.DrawWireCube(transform.position + Offset, Size);

			for (var x = 0; x < nodes.Length; x++)
			{
				for (var y = 0; y < nodes[x].Length; y++)
				{
					for (var z = 0; z < nodes[x][y].Length; z++)
					{
						var node = nodes[x][y][z];
						if ((node.Availability & DrawFlags) == 0)
							continue;
						
						if (DrawConnections)
						{
							foreach (var pair in node.Connections)
							{
								if (pair.Value < 1.01f)
								{
									Gizmos.color = Color.yellow;
								}
								else if (pair.Value > 1.01f)
								{
									Gizmos.color = new Color(1f, 0.5f, 0f);
								}
								else
								{
									Gizmos.color = Color.green;
								}
								
								Gizmos.DrawLine(node.Position, pair.Key.Position);
							}
						}
						
						if (DrawNodes)
						{
							if (node.Availability == ENodeAvailability.Available)
							{
								Gizmos.color = Color.green;
							}
							else if ((node.Availability & ENodeAvailability.InsideObject) != 0)
							{
								Gizmos.color = Color.red;
							}
							else if ((node.Availability & ENodeAvailability.NoConnections) != 0)
							{
								Gizmos.color = Color.yellow;
							}
							
							Gizmos.DrawSphere(node.Position, 0.1f);
						}
					}
				}
			}
			
			if (DrawPath && resultingPath != null && resultingPath.Count > 1)
			{
				Gizmos.color = Color.cyan;
				
				for (var i = 0; i < resultingPath.Count - 1; i++)
					Gizmos.DrawLine(resultingPath[i].Position, resultingPath[i + 1].Position);
			}
		}
#endif
		
		#endregion

		public void CreateGrid()
		{
			var position = transform.position + Offset - Size / 2f;
			
			xSize = (int)(Size.x / Distance) + 1;
			ySize = (int)(Size.y / Distance) + 1;
			zSize = (int)(Size.z / Distance) + 1;

			nodes = new Node[xSize][][];
			for (var i = 0; i < xSize; i++)
			{
				nodes[i] = new Node[ySize][];
				for (var k = 0; k < ySize; k++)
					nodes[i][k] = new Node[zSize];
			}

			for (var x = 0; x < xSize; x++)
			{
				for (var y = 0; y < ySize; y++)
				{
					for (var z = 0; z < zSize; z++)
					{
						var node = new Node();
						node.Position = new Vector3(x * Distance, y * Distance, z * Distance) + position;
						node.Availability = ENodeAvailability.Available;
						node.Connections = new Dictionary<Node, float>();
						
						nodes[x][y][z] = node;
					}
				}
			}
			
			if (FilterInsideObjects)
				findInsideObjects();
			
			findNeighborConnections();
		}

		public Node FindClosestNode(Vector3 position)
		{
			Node closestNode = null;
			var closestDistance = Mathf.Infinity;
			
			for (var x = 0; x < xSize; x++)
			{
				var xArray = nodes[x];
				for (var y = 0; y < ySize; y++)
				{
					var yArray = xArray[y];
					for (var z = 0; z < zSize; z++)
					{
						var node = yArray[z];
						
						var dist = Vector3.Distance(node.Position, position) / Distance;
						if (dist > closestDistance)
							continue;
						
						closestDistance = dist;
						closestNode = node;
					}
				}
			}
			
			return closestNode;
		}

		public List<Node> FindPath(Vector3 start, Vector3 end)
		{
			searchedNodes.Clear();
			toSearchNodes.Clear();
			resultingPath.Clear();
			
			for (var x = 0; x < xSize; x++)
			{
				var xArray = nodes[x];
				for (var y = 0; y < ySize; y++)
				{
					var yArray = xArray[y];
					for (var z = 0; z < zSize; z++)
					{
						yArray[z].ClearPathCalculations();
					}
				}
			}
			
			var distanceBetweenPoints = Vector3.Distance(start, end) / Distance;

			toSearchNodes.Add(FindClosestNode(start));
			
			var endNode = FindClosestNode(end);

			var startNode = toSearchNodes[0];
			startNode.GCost = 0f;
			startNode.HCost = distanceBetweenPoints;
			startNode.FCost = distanceBetweenPoints;
			
			while (toSearchNodes.Count > 0)
			{
				var node = toSearchNodes[0];

				for (var i = 0; i < toSearchNodes.Count; i++)
				{
					var searchingNode = toSearchNodes[i];
					if (searchingNode.FCost < node.FCost || searchingNode.FCost == node.FCost && searchingNode.HCost < node.HCost)
						node = searchingNode;
				}

				toSearchNodes.Remove(node);
				searchedNodes.Add(node);

				if (node == endNode)
				{
					while (endNode != startNode)
					{
						resultingPath.Add(endNode);
						endNode = endNode.Connection;
					}
			
					resultingPath.Add(startNode);
					return resultingPath;
				}
				
				calculateNeighbors(node, end);
			}

			return null;
		}
		
		#region Internals

		private void findInsideObjects()
		{
			var renderers = GetComponentsInChildren<Renderer>();
			for (var i = 0; i < renderers.Length; i++)
			{
				var rend = renderers[i];
				
				// Only check objects that are in the blocking mask
				if ((FilterMask.value & (1 << rend.gameObject.layer)) == 0)
					continue;

				var bounds = rend.bounds;

				for (var x = 0; x < nodes.Length; x++)
				{
					var xArray = nodes[x];
					for (var y = 0; y < xArray.Length; y++)
					{
						var yArray = xArray[y];
						for (var z = 0; z < yArray.Length; z++)
						{
							var node = yArray[z];
							
							// If a node is inside bounds of an object, mark it unavailable
							if (!bounds.Contains(node.Position))
								continue;

							var availability = node.Availability;
							
							// Remove available flag
							availability &= ~ENodeAvailability.Available;
							
							// Add inside object flag
							availability |= ENodeAvailability.InsideObject;

							node.Availability = availability;
						}
					}
				}
			}
		}

		private void findNeighborConnections()
		{
			for (var x = 0; x < nodes.Length; x++)
			{
				var xArray = nodes[x];
				for (var y = 0; y < xArray.Length; y++)
				{
					var yArray = xArray[y];
					for (var z = 0; z < yArray.Length; z++)
					{
						var node = yArray[z];
						
						getConnectionsAndCosts(node, x, y, z);
						
						// If a node does not have any connections, mark it unavailable
						if (node.Connections.Count != 0)
							continue;
						
						var availability = node.Availability;
							
						// Remove available flag
						availability &= ~ENodeAvailability.Available;
							
						// Add no connections flag
						availability |= ENodeAvailability.NoConnections;

						node.Availability = availability;
					}
				}
			}
		}

		private void getConnectionsAndCosts(Node node, int x, int y, int z)
		{
			for (var cX = -1; cX < 2; cX++)
			{
				for (var cY = -1; cY < 2; cY++)
				{
					for (var cZ = -1; cZ < 2; cZ++)
					{
						var neighborX = x + cX;
						if (neighborX < 0 || neighborX >= xSize) 
							continue;
						
						var neighborY = y + cY;
						if (neighborY < 0 || neighborY >= ySize) 
							continue;

						var neighborZ = z + cZ;
						if (neighborZ < 0 || neighborZ >= zSize) 
							continue;
						
						var neighborNode = nodes[neighborX][neighborY][neighborZ];

						// Don't check connections to itself
						if (neighborNode == node)
							continue;
						
						// Already checked to be a neighbor
						if (node.Connections.ContainsKey(neighborNode))
							continue;

						var nodePos = node.Position;
						var neighborPos = neighborNode.Position;
						
						if (Physics.Raycast(nodePos, neighborPos - nodePos, float.MaxValue, FilterMask))
							continue;

						var cost = Vector3.Distance(nodePos, neighborPos) / Distance;
						
						node.Connections[neighborNode] = cost;
						neighborNode.Connections[node] = cost;
					}
				}
			}
		}

		private void calculateNeighbors(Node node, Vector3 end)
		{
			foreach (var pair in node.Connections)
			{
				var neighborNode = pair.Key;
				if (neighborNode.Availability != ENodeAvailability.Available)
					continue;
				
				if (searchedNodes.Contains(neighborNode))
					continue;

				var gCost = node.GCost + pair.Value;
				if (gCost < neighborNode.GCost)
				{
					var hCost = Vector3.Distance(neighborNode.Position, end) / Distance;
					
					neighborNode.Connection = node;
					neighborNode.GCost = gCost;
					neighborNode.HCost = hCost;
					neighborNode.FCost = gCost + hCost;
				
					toSearchNodes.Add(neighborNode);
				}
			}
		}

		#endregion

		public class Node
		{
			public Vector3 Position;
			public ENodeAvailability Availability;

			public Dictionary<Node, float> Connections;
			
			#region Path Calculation

			public float GCost = float.MaxValue;
			public float HCost;
			public float FCost;

			public Node Connection;
			
			public void ClearPathCalculations()
			{
				GCost = float.MaxValue;
				HCost = 0f;
				FCost = 0f;
				Connection = null;
			}

			#endregion
		}

		[Flags]
		public enum ENodeAvailability
		{
			None = 0,
			Available = 1,
			InsideObject = 2,
			NoConnections = 4
		}
	}
}