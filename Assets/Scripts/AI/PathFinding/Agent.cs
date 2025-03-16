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
		
		public Grid Grid { get; private set; }

		public Obstacle Obstacle { get; private set; }

		public Vector3 CurrentNode { get; private set; }
		
		public Vector3 NextNode { get; private set; }
		
		public Vector3 LastNode { get; private set; }
		
		public bool PathPending { get; private set; }
		
		public bool HasPath => Path != null;

		public Path Path { get; private set; }

		public float RemainingDistance => Vector3.Distance(thisTr.position, LastNode);
		
		private Transform thisTr;
		
		private int activeIdentifier;

		private int nextNodeIndex;
		
		public void Awake()
		{
			thisTr = transform;

			// todo: find closest grid and handle changes
			Grid = FindAnyObjectByType<Grid>();
			
			// todo: use this and filter it out for finding path so the agent isnt blocking itself
			Obstacle = GetComponent<Obstacle>();
		}

		public void Update()
		{
			if (!HasPath)
				return;

			var distance = Vector3.Distance(thisTr.position, NextNode);
			if (distance > StoppingDistance)
				return;

			if (NextNode == LastNode)
			{
				ClearDestination();
				return;
			}
			
			nextNodeIndex++;

			CurrentNode = Path.Points[nextNodeIndex - 1];
			NextNode = Path.Points[nextNodeIndex];
		}

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
			if (path == null)
			{
				Debug.Log($"[Agent] Received null path expecting identifier {activeIdentifier}");
				return;
			}
			
			if (activeIdentifier != path.Identifier)
			{
				Debug.Log($"[Agent] Discarding path with identifier {path.Identifier} as it doesn't match the current identifier {activeIdentifier}");
				return;
			}

			Path = path;
			nextNodeIndex = 0;
			
			CurrentNode = thisTr.position;
			NextNode = Path.Points[nextNodeIndex];
			LastNode = Path.Points[^1];
			
			PathPending = false;
		}
	}
}