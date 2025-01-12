using System.Collections.Generic;
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

		public bool HasReachedPoint()
		{
			if (CurrentPath == null || CurrentPath.Points.Count <= CurrentPoint)
				return true;
			
			return Vector3.Distance(owner.GetTransform().position, CurrentPath.Points[CurrentPoint]) <= owner.Agent.stoppingDistance + ((NPCData)owner.Data).PatrolReachRange;
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

		public List<Vector3> GetPoints()
		{
			return CurrentPath == null ? null : CurrentPath.Points;
		}

		public int GetClosestPoint()
		{
			var points = GetPoints();
			if (points == null)
				return -1;
			
			var pos = owner.GetTransform().position;

			var closestIndex = -1;
			var closestDistance = float.MaxValue;
			
			for (var i = 0; i < points.Count; i++)
			{
				var distance = Vector3.Distance(pos, points[i]);
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

			owner.Walk(CurrentPath.Points[CurrentPoint]);
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