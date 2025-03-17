using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.PathFinding
{
	[ExecuteInEditMode]
	public class Agent : MonoBehaviour
	{
		[SerializeField]
		public float StoppingDistance;
		
		[SerializeField]
		public float Radius = 1f;

		public Vector3 CurrentNode { get; private set; }
		
		public Vector3 NextNode { get; private set; }
		
		public Vector3 LastNode { get; private set; }
		
		public bool PathPending { get; private set; }
		
		public int BeforeSkipNextNodeIndex { get; private set; }

		public bool HasPath => Path != null;
		public bool HasGrid => Grid != null;

		public Path Path { get; private set; }

		public Grid Grid { get; private set; }

		public float RemainingDistance => Vector3.Distance(thisTr.position, LastNode);
		
		public static readonly List<Agent> Agents = new ();

		private Transform thisTr;
		
		private int activeIdentifier;

		private int nextNodeIndex;
		
		public void Awake()
		{
			thisTr = transform;

			// todo: find closest grid and handle changes
			Grid = FindAnyObjectByType<Grid>();
		}

		public void OnEnable()
		{
			Agents.Add(this);
		}

		public void OnDisable()
		{
			Agents.Remove(this);
		}
		
		public void Update()
		{
			if (!HasPath)
				return;

			var agentPos = thisTr.position;
			
			var distance = Vector3.Distance(agentPos, NextNode);
			if (distance > StoppingDistance)
				return;

			if (NextNode == LastNode)
			{
				ClearDestination();
				return;
			}

			var points = Path.Points;
			
			nextNodeIndex++;

			CurrentNode = points[nextNodeIndex - 1];
			NextNode = points[nextNodeIndex];

			BeforeSkipNextNodeIndex = nextNodeIndex;
			
			if (NextNode == LastNode)
				return;
			
			skipUnnecessaryPoints();
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.DrawWireSphere(transform.position, Radius);
		}
#endif

		public void SetDestination(Vector3 destination)
		{
			PathPending = true;

			activeIdentifier = Random.Range(int.MinValue, int.MaxValue);
			Grid.FindPath(thisTr.position, destination, activeIdentifier, onPathFound).Forget();
		}

		public void ClearDestination()
		{
			Path = null;
		}

		private void onPathFound(Path path)
		{
			if (this == null)
				return;
			
			if (path == null)
			{
				Debug.LogWarning($"[Agent] Received null path expecting identifier {activeIdentifier}");
				Path = null;
				PathPending = false;
				return;
			}
			
			if (activeIdentifier != path.Identifier)
			{
				Debug.LogWarning($"[Agent] Discarding path with identifier {path.Identifier} as it doesn't match the current identifier {activeIdentifier}");
				return;
			}

			if (path.Points.Length == 0)
			{
				Debug.LogWarning($"[Agent] Discarding path with identifier {path.Identifier} as it has 0 points");
				Path = null;
				PathPending = false;
				return;
			}

			Path = path;
			nextNodeIndex = 0;
			
			CurrentNode = thisTr.position;
			NextNode = Path.Points[nextNodeIndex];
			LastNode = Path.Points[^1];
			
			BeforeSkipNextNodeIndex = nextNodeIndex;

			PathPending = false;
		}

		private void skipUnnecessaryPoints()
		{
			var points = Path.Points;
			var agentPos = thisTr.position;

			while (NextNode != LastNode)
			{
				var direction = points[nextNodeIndex + 1] - agentPos;
				var distance = direction.magnitude;
				
				if (Physics.Raycast(agentPos, direction, distance, Grid.FilterMask))
					break;
				
				if (Physics.SphereCast(agentPos, Grid.Radius, direction, out _, distance, Grid.FilterMask))
					break;
				
				nextNodeIndex++;

				CurrentNode = points[nextNodeIndex - 1];
				NextNode = points[nextNodeIndex];
			}
		}
	}
}