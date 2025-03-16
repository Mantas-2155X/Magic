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
		
		// todo: use this and filter it out for finding path so the agent isnt blocking itself
		[SerializeField]
		public Obstacle Obstacle;

		// todo: find closest grid and handle changes
		[SerializeField]
		public Grid Grid;

		public Vector3 CurrentNode { get; private set; }
		
		public Vector3 NextNode { get; private set; }
		
		public Vector3 LastNode { get; private set; }
		
		public bool PathPending { get; private set; }
		
		public bool HasPath => activePath != null;

		public float RemainingDistance => Vector3.Distance(thisTr.position, LastNode);
		
		private Transform thisTr;
		
		private Path activePath;
		private int activeIdentifier;

		private int nextNodeIndex;
		
		public void Awake()
		{
			thisTr = transform;
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

			CurrentNode = activePath.Points[nextNodeIndex - 1];
			NextNode = activePath.Points[nextNodeIndex];
		}

		public void SetDestination(Vector3 destination)
		{
			PathPending = true;

			activeIdentifier = Random.Range(int.MinValue, int.MaxValue);
			Grid.FindPath(thisTr.position, destination, activeIdentifier, onPathFound).Forget();
		}

		public void ClearDestination()
		{
			activePath = null;
		}

		private void onPathFound(Path path)
		{
			if (activeIdentifier != path.Identifier)
			{
				Debug.Log($"[Agent] Discarding path with identifier {path.Identifier} as it doesn't match the current identifier {activeIdentifier}");
				return;
			}

			activePath = path;
			nextNodeIndex = 0;
			
			CurrentNode = thisTr.position;
			NextNode = activePath.Points[nextNodeIndex];
			LastNode = activePath.Points[^1];
			
			PathPending = false;
		}
	}
}