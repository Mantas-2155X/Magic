using System.Collections.Generic;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Patrol
	{
		private readonly NPC owner;
		
		public Patrol(NPC owner)
		{
			this.owner = owner;
		}
		
		public int CurrentPoint { get; private set; }

		private List<Vector3> path = new ();

		public bool HasReachedPoint()
		{
			if (path.Count == 0)
				return true;
			
			return Vector3.Distance(owner.GetTransform().position, path[CurrentPoint]) <= owner.Agent.stoppingDistance + owner.PatrolReachRange;
		}
		
		public void SetPoints(List<Vector3> points, int startAt)
		{
			path = points;

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
			return path;
		}

		public int GetClosestPoint()
		{
			var pos = owner.GetTransform().position;

			var closestIndex = -1;
			var closestDistance = float.MaxValue;
			
			for (var i = 0; i < path.Count; i++)
			{
				var distance = Vector3.Distance(pos, path[i]);
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
			if (path.Count == 0)
				return;

			owner.Walk(path[CurrentPoint]);
		}
		
		public void GoToNextPoint()
		{
			if (path.Count == 0)
				return;
			
			CurrentPoint++;
			
			if (CurrentPoint >= path.Count)
				CurrentPoint = 0;
			
			GoToCurrentPoint();
		}

		public void GoToPreviousPoint()
		{
			if (path.Count == 0)
				return;
			
			CurrentPoint--;
			
			if (CurrentPoint < 0)
				CurrentPoint = path.Count - 1;
			
			GoToCurrentPoint();
		}
	}
}