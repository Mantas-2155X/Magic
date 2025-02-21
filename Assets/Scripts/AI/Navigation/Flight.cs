using AI.AIModes;
using AI.Enums;
using Tools;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AI.Navigation
{
	public class Flight : MonoBehaviour
	{
		[SerializeField]
		public NPC NPC;

		[SerializeField]
		public float StayBelow;
		
		[SerializeField]
		public float StayAbove;

		[SerializeField]
		public float HoverRange;
		
		[SerializeField]
		public float HoverSpeed;

		[SerializeField]
		public float FlightSpeed;

		[SerializeField]
		public float RotateSpeed;
		
		[SerializeField]
		public float StabilizeSpeed;
		
		private Rigidbody rb;
		
		private float angleStuckTime;

		private Vector3 movementTarget;
		
		private readonly Vector3[] corners = new Vector3[2];
		
		public void Awake()
		{
			rb = NPC.Body.Rigidbody;
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			var path = NPC.Agent.path;
			if (path == null)
				return;

			var previousColor = Gizmos.color;
			Gizmos.color = Color.blue;
			
			var allCorners = path.corners;
			for (var i = 0; i < allCorners.Length; i++)
			{
				var corner = allCorners[i];
				corner.y += HoverRange;
				
				Gizmos.DrawSphere(corner, 0.1f);
				
				if (i != allCorners.Length - 1)
				{	
					var nextCorner = allCorners[i];
					nextCorner.y += HoverRange;
					
					Gizmos.DrawLine(corner, nextCorner);
				}
			}
			
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(movementTarget, 0.1f);
			
			Gizmos.color = previousColor;
		}
#endif
		
		public void Update()
		{
			var euler = rb.rotation.eulerAngles;
			
			var x = euler.x;
			var z = euler.z;

			if (x > 180f)
				x -= 360f;
			
			if (z > 180f)
				z -= 360f;

			if (Mathf.Abs(x) < 20f && Mathf.Abs(z) < 20f)
			{
				angleStuckTime = 0f;
				return;
			}

			angleStuckTime += Time.deltaTime;
			
			if (angleStuckTime < 2f)
				return;
			
			rb.AddRelativeTorque(new Vector3(Random.Range(0f, 5f), Random.Range(0f, 5f), Random.Range(0f, 5f)) * 10f, ForceMode.VelocityChange);
			angleStuckTime = 0f;
			
			NPC.Chase.ResetChaseRange(true);
		}

		public void FixedUpdate()
		{
			var position = rb.position;
			var rotation = rb.rotation;
			
			var distanceToCeiling = GetCeilingDistance(position);
			var distanceToFloor = GetFloorDistance(position);
			
			Hover(position, distanceToCeiling, distanceToFloor);

			var agent = NPC.Agent;
			agent.nextPosition = position;

			if (NPC.Paralyzed)
			{
				rb.AddTorque(new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5)), ForceMode.Impulse);
				return;
			}
			
			var aiMode = NPC.AIMode;
			if (aiMode == EAIMode.Walking)
			{
				if (agent.enabled && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete)
				{
					var path = agent.path;
					path.GetCornersNonAlloc(corners);

					movementTarget = corners[1];
				}
				else
				{
					movementTarget = NPC.Destination;
				}
			
				// Don't hover too high when going to target
				if (HoverRange > distanceToCeiling - StayBelow)
					movementTarget.y += distanceToCeiling - StayBelow;
				else
					movementTarget.y += HoverRange;

				if (movementTarget.y > position.y)
					movementTarget.y += movementTarget.y - position.y;
				else if (movementTarget.y < position.y)
					movementTarget.y -= position.y - movementTarget.y;
#if UNITY_EDITOR
				Debug.DrawLine(position, movementTarget, new Color(0.25f, 0.5f, 0.75f));
#endif
				FlyTowards(movementTarget);
				RotateTowards(movementTarget);
			}
			else
			{
				var rotating = false;
				
				if (aiMode == EAIMode.Action)
				{
					var attackTarget = NPC.AttackTargetTransform;
					var otherTarget = NPC.OtherTargetTransform;

					if (attackTarget != null)
					{
						RotateTowards(attackTarget.position);
						rotating = true;
					}
					else if (otherTarget != null)
					{
						RotateTowards(otherTarget.position);
						rotating = true;
					}
				}

				if (!rotating)
					Stabilize(rotation);
			}
		}

		#region Flight

		public void Hover(Vector3 currentPosition, float distanceToCeiling, float distanceToFloor)
		{
			var flightTarget = currentPosition;
			
			if (distanceToCeiling < StayBelow && distanceToFloor < StayAbove)
			{
				// Too close to both ceiling and floor, try to stabilize in the middle
				if (distanceToCeiling < distanceToFloor)
					flightTarget.y -= distanceToFloor - distanceToCeiling;
				else
					flightTarget.y += distanceToCeiling - distanceToFloor;
			}
			else if (distanceToCeiling < StayBelow)
			{
				// Too close to ceiling, try to go down
				flightTarget.y -= StayBelow - distanceToCeiling;
			}
			else if (distanceToFloor < StayAbove)
			{
				// Too close to floor, try to go up
				flightTarget.y += StayAbove - distanceToFloor;
			}
			else
			{
				if (Mathf.Approximately(distanceToCeiling, float.MaxValue) && Mathf.Approximately(distanceToFloor, float.MaxValue))
				{
					// No ceiling or floor found to base position on, keep hovering
					return;
				}

				var isFlightStuck = ((Walking)NPC.AIModes[EAIMode.Walking]).IsFlightStuck;
				
				if (Mathf.Approximately(distanceToCeiling, float.MaxValue))
				{
					// No ceiling found, try to be within ground range
					if (distanceToFloor > HoverRange && !isFlightStuck)
						flightTarget.y -= distanceToFloor - HoverRange;
				}
				else if (Mathf.Approximately(distanceToFloor, float.MaxValue))
				{
					// No floor found, try to be within ceiling range
					if (distanceToCeiling > HoverRange && !isFlightStuck)
						flightTarget.y += distanceToCeiling - HoverRange;
				}
				else
				{
					// Not too close to anything, keep hovering
					return;
				}
			}
			
			var force = flightTarget - currentPosition;
			rb.AddForce(force * HoverSpeed, ForceMode.Acceleration);
		}

		public void Stabilize(Quaternion currentRotation)
		{
			var euler = currentRotation.eulerAngles;
			
			var x = euler.x;
			var z = euler.z;

			if (x > 180f)
				x -= 360f;
			
			if (z > 180f)
				z -= 360f;
			
			if (Mathf.Abs(x) < 5f && Mathf.Abs(z) < 5f)
				return;
			
			var deltaRotation = Quaternion.Euler(0f, euler.y, 0f) * Quaternion.Inverse(currentRotation);
			deltaRotation.ToAngleAxis(out var angle, out var axis);
			
			if (angle > 180f)
				angle -= 360f;

			if (Mathf.Approximately(angle, 0)) 
				return;
			
			angle *= Mathf.Deg2Rad;

			var torque = axis * angle;
			rb.AddTorque(torque * StabilizeSpeed, ForceMode.VelocityChange);
		}

		public void FlyTowards(Vector3 target)
		{
			rb.AddForce((target - rb.position).normalized * FlightSpeed, ForceMode.Acceleration);
		}
		
		public void RotateTowards(Vector3 target)
		{
			var pos1 = target;
			var pos2 = rb.position;
			
			pos1.y = 0;
			pos2.y = 0;
			
			var targetPosition = pos1 - pos2;
			if (targetPosition.magnitude < 1f)
				return;
			
			var targetRotation = Quaternion.LookRotation(targetPosition, Vector3.up);
			
			var deltaRotation = targetRotation * Quaternion.Inverse(rb.rotation);
			deltaRotation.ToAngleAxis(out var angle, out var axis);
			
			if (angle > 180f)
				angle -= 360f;

			if (Mathf.Approximately(angle, 0)) 
				return;
			
			angle *= Mathf.Deg2Rad;

			var torque = axis * angle;
			rb.AddTorque(torque * RotateSpeed, ForceMode.Acceleration);
		}
		
		#endregion

		public float GetCeilingDistance(Vector3 position)
		{
			if (!Physics.Raycast(position, Vector3.up, out var hit, float.MaxValue, ~LayerMaskTools.GetMaskWithNPC(), QueryTriggerInteraction.Ignore))
				return float.MaxValue;

			return hit.distance;
		}

		public float GetFloorDistance(Vector3 position)
		{
			if (!Physics.Raycast(position, Vector3.down, out var hit, float.MaxValue, ~LayerMaskTools.GetMaskWithNPC(), QueryTriggerInteraction.Ignore))
				return float.MaxValue;

			return hit.distance;
		}
	}
}