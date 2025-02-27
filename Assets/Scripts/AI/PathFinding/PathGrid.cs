using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AI.PathFinding
{
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
		public bool FilterUnconnected = true;
		
		[SerializeField]
		public LayerMask FilterMask = -1;

		[Header("Draw Settings")]
		public bool DrawBounds = true;
		
		public bool DrawNodes = true;

		public bool DrawConnections = true;

		public ENodeAvailability DrawFlags = (ENodeAvailability)~0;
		
		private Node[][][] nodes;
		
		private int xSize;
		private int ySize;
		private int zSize;

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
		}
#endif
		
		public void CreateGrid()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			
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
			
			stopwatch.Stop();
			Debug.Log($"Creating grid took {stopwatch.ElapsedMilliseconds}ms");
			
			if (FilterInsideObjects)
				FindInsideObjects();
			
			if (FilterUnconnected)
				FindNeighborConnections();
		}

		public void FindInsideObjects()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

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
			
			stopwatch.Stop();
			Debug.Log($"Inside objects filtering took {stopwatch.ElapsedMilliseconds}ms");
		}

		public void FindNeighborConnections()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			for (var x = 0; x < nodes.Length; x++)
			{
				var xArray = nodes[x];
				for (var y = 0; y < xArray.Length; y++)
				{
					var yArray = xArray[y];
					for (var z = 0; z < yArray.Length; z++)
					{
						var node = yArray[z];
						
						getConnections(node, x, y, z);
						
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
			
			stopwatch.Stop();
			Debug.Log($"Finding neighbor connections took {stopwatch.ElapsedMilliseconds}ms");
		}

		private void getConnections(Node node, int x, int y, int z)
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

		public class Node
		{
			public Vector3 Position;

			public ENodeAvailability Availability;

			public Dictionary<Node, float> Connections;
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