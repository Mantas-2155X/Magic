using AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace AI.PathFinding
{
	public class AgentCompat : MonoBehaviour
	{
		[SerializeField]
		public NavMeshAgent NavMeshAgent;
		
		[SerializeField]
		public Agent Agent;

		[SerializeField]
		public Flight Flight;

		[SerializeField]
		public Rigidbody Rigidbody;

		public bool IsNavMesh => NavMeshAgent != null;
		public bool HasFlight => Flight != null;

		#region NavMeshAgent only

		public bool UpdatePosition
		{
			get => IsNavMesh && NavMeshAgent.updatePosition;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.updatePosition = value;
			}
		}

		public bool UpdateRotation
		{
			get => IsNavMesh && NavMeshAgent.updateRotation;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.updateRotation = value;
			}
		}
		
		public bool UpdateUpAxis
		{
			get => IsNavMesh && NavMeshAgent.updateUpAxis;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.updateUpAxis = value;
			}
		}
		
		public bool IsOnOffMeshLink
		{
			get => IsNavMesh && NavMeshAgent.isOnOffMeshLink;
		}
		
		public OffMeshLinkData CurrentOffMeshLinkData => IsNavMesh ? NavMeshAgent.currentOffMeshLinkData : default;

		public float BaseOffset
		{
			get => IsNavMesh ? NavMeshAgent.baseOffset : 0f;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.baseOffset = value;
			}
		}
		
		public float Speed
		{
			get => IsNavMesh ? NavMeshAgent.speed : 0f;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.speed = value;
			}
		}
		
		public float AngularSpeed
		{
			get => IsNavMesh ? NavMeshAgent.angularSpeed : 0f;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.angularSpeed = value;
			}
		}

		public void CompleteOffMeshLink()
		{
			if (IsNavMesh)
				NavMeshAgent.CompleteOffMeshLink();
		}

		public void Warp(Vector3 position)
		{
			if (IsNavMesh)
				NavMeshAgent.Warp(position);
		}
		
		#endregion

		#region Mixed

		public Vector3 Velocity
		{
			get => IsNavMesh ? NavMeshAgent.velocity : Rigidbody.linearVelocity;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.velocity = value;
				else
					Rigidbody.linearVelocity = value;
			}
		}
		
		public float StoppingDistance
		{
			get => IsNavMesh ? NavMeshAgent.stoppingDistance : Agent.StoppingDistance;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.stoppingDistance = value;
				else
					Agent.StoppingDistance = value;
			}
		}
		
		public float RemainingDistance => IsNavMesh ? NavMeshAgent.remainingDistance : Agent.RemainingDistance;

		public float Radius => IsNavMesh ? NavMesh.GetSettingsByID(NavMeshAgent.agentTypeID).agentRadius : Agent.Radius;

		public Vector3 Destination
		{
			get => IsNavMesh ? NavMeshAgent.destination : Agent.LastNode;
			set
			{
				if (IsNavMesh)
					NavMeshAgent.destination = value;
				else
					Agent.SetDestination(value);
			}
		}
		
		public void SetDestination(Vector3 destination)
		{
			if (IsNavMesh)
				NavMeshAgent.SetDestination(destination);
			else
				Agent.SetDestination(destination);
		}

		public bool IsOnNavMesh => IsNavMesh ? NavMeshAgent.isOnNavMesh : Agent.HasGrid;
		
		public bool HasPath => IsNavMesh ? NavMeshAgent.hasPath : Agent.HasPath;
		
		public bool PathPending => IsNavMesh ? NavMeshAgent.pathPending : Agent.PathPending;

		#endregion

		#region Agent only
		
		

		#endregion
	}
}