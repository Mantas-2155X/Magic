using ScriptableObjects;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Patrolling
	{
		private readonly NPC owner;
		
		public Patrolling(NPC owner)
		{
			this.owner = owner;
		}
		
		public int CurrentPoint { get; private set; }
		public Path CurrentPath { get; private set; }

		public float WaitUntil { get; private set; } = -1f;
		public bool WaitOnArrival { get; private set; } = true;
		
		// (Reached, Waiting)
		public (bool, bool) HasReachedPoint()
		{
			if (CurrentPath == null || CurrentPath.Points.Count <= CurrentPoint)
				return (true, false);
			
			var reached = Vector3.Distance(owner.GetTransform().position, CurrentPath.Points[CurrentPoint].Point) <= owner.Agent.stoppingDistance + ((NPCData)owner.Data).PatrolReachRange;
			if (!reached)
				return (false, false);

			// If point requires waiting, handle that here
			if (WaitOnArrival)
			{
				if (WaitUntil < 0f)
				{
					var pauseLength = CurrentPath.Points[CurrentPoint].Pause;
					if (pauseLength > 0f)
					{
						WaitUntil = Time.time + pauseLength;
						return (true, true);
					}
				}
				
				if (Time.time < WaitUntil)
					return (true, true);
			
				WaitUntil = -1f;
				WaitOnArrival = false;
			}
			
			return (true, false);
		}
		
		public void SetPath(Path path, int startAt)
		{
			CurrentPath = path;

			if (startAt == -1)
			{
				startAt = GetClosestPoint();
				
				if (startAt == -1)
					startAt = 0;
			}
			
			CurrentPoint = startAt;
		}

		public int GetClosestPoint()
		{
			var points = CurrentPath == null ? null : CurrentPath.Points;
			if (points == null)
				return -1;
			
			var pos = owner.GetTransform().position;

			var closestIndex = -1;
			var closestDistance = float.MaxValue;
			
			for (var i = 0; i < points.Count; i++)
			{
				var distance = Vector3.Distance(pos, points[i].Point);
				if (distance < closestDistance)
				{
					closestIndex = i;
					closestDistance = distance;
				}
			}
			
			return closestIndex;
		}
		
		public void GoToCurrentPoint()
		{
			if (CurrentPath == null || CurrentPath.Points.Count <= CurrentPoint)
				return;

			var pathPoint = CurrentPath.Points[CurrentPoint];
			if (pathPoint.Pause > 0f)
				WaitOnArrival = true;
			
			owner.Walk(pathPoint.Point);
		}
		
		public void GoToNextPoint()
		{
			if (CurrentPath == null)
				return;
			
			CurrentPoint++;
			
			if (CurrentPoint >= CurrentPath.Points.Count)
				CurrentPoint = 0;
			
			GoToCurrentPoint();
		}

		public void GoToPreviousPoint()
		{
			if (CurrentPath == null)
				return;
			
			CurrentPoint--;
			
			if (CurrentPoint < 0)
				CurrentPoint = CurrentPath.Points.Count - 1;
			
			GoToCurrentPoint();
		}
	}
}