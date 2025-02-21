using Tools;
using UnityEngine;

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
		public float StabilizeSpeed;
		
		[SerializeField]
		public float AimAtSpeed;

		private Rigidbody rb;
		
		public void Awake()
		{
			rb = NPC.Body.Rigidbody;
		}

		public void FixedUpdate()
		{
			var position = rb.position;
			var rotation = rb.rotation;
			
			var distanceToCeiling = GetCeilingDistance(position);
			var distanceToFloor = GetFloorDistance(position);
			
			Hover(position, distanceToCeiling, distanceToFloor);
			Stabilize(rotation);
		}

		#region Flight

		public void Hover(Vector3 position, float distanceToCeiling, float distanceToFloor)
		{
			var flightTarget = position;
			
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
				
				if (Mathf.Approximately(distanceToCeiling, float.MaxValue))
				{
					// No ceiling found, try to be within ground range
					if (distanceToFloor > HoverRange)
						flightTarget.y -= distanceToFloor - HoverRange;
				}
				else if (Mathf.Approximately(distanceToFloor, float.MaxValue))
				{
					// No floor found, try to be within ceiling range
					if (distanceToCeiling > HoverRange)
						flightTarget.y += distanceToCeiling - HoverRange;
				}
				else
				{
					// Not too close to anything, keep hovering
					return;
				}
			}
			
			var force = flightTarget - position;
			rb.AddForce(force * HoverSpeed, ForceMode.Acceleration);
		}

		public void Stabilize(Quaternion rotation)
		{
			var euler = transform.eulerAngles;
			
			var x = euler.x;
			var z = euler.z;

			if (x > 180f)
				x -= 360f;
			
			if (z > 180f)
				z -= 360f;
			
			if (Mathf.Abs(x) < 5f && Mathf.Abs(z) < 5f)
				return;
			
			var deltaRotation = Quaternion.Euler(0f, euler.y, 0f) * Quaternion.Inverse(rotation);
			deltaRotation.ToAngleAxis(out var angle, out var axis);
			
			if (angle > 180f)
				angle -= 360f;

			if (Mathf.Approximately(angle, 0)) 
				return;
			
			angle *= Mathf.Deg2Rad;

			var torque = axis * angle;
			rb.AddTorque(torque * StabilizeSpeed, ForceMode.VelocityChange);
		}

		public (float, bool) AimAt(Transform target)
		{
			var pos1 = target.position;
			var pos2 = rb.position;
			
			var verticalDistance = pos1.y - pos2.y;
			
			pos1.y = 0;
			pos2.y = 0;
			
			var targetPosition = pos1 - pos2;
			if (targetPosition.magnitude < 1f)
				return (verticalDistance, true);
			
			var targetRotation = Quaternion.LookRotation(targetPosition, Vector3.up);
			
			var deltaRotation = targetRotation * Quaternion.Inverse(rb.rotation);
			deltaRotation.ToAngleAxis(out var angle, out var axis);
			
			if (angle > 180f)
				angle -= 360f;

			if (Mathf.Approximately(angle, 0)) 
				return (verticalDistance, false);
			
			angle *= Mathf.Deg2Rad;

			var torque = axis * angle;
			rb.AddTorque(torque * (AimAtSpeed * Time.fixedDeltaTime), ForceMode.VelocityChange);

			return (verticalDistance, false);
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