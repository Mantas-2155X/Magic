using AI.Enums;
using ScriptableObjects;
using Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI
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
		public float HoverSpeed;

		[SerializeField]
		public float FlightSpeed;

		[SerializeField]
		public float RotateSpeed;
		
		[SerializeField]
		public float StabilizeSpeed;
		
		[SerializeField]
		public bool UseSpellRange = true;
		
		[SerializeField]
		public bool AngleAntiStuckEnabled = true;

		[SerializeField]
		public float AngleStuckDegree = 25f;

		[SerializeField]
		public float AngleStuckTime = 2.5f;
		
		[SerializeField]
		public bool PositionRaycastAntiStuckEnabled = true;

		[SerializeField]
		public float PositionStuckDistance = 0.4f;
		
		[SerializeField]
		public bool PositionVelocityAntiStuckEnabled = true;
		
		[SerializeField]
		public float PositionStuckVelocity = 0.75f;

		[SerializeField]
		public float PositionStuckTime = 0.75f;

		[SerializeField]
		public float PositionStuckRecalculateAfter = 0.15f;
		
		private Rigidbody rb;
		
		private float angleStuckDuration;
		private float positionStuckDuration;

		private float lastPositionStuck;

		private Vector3? movementTarget;
		
		#region MonoBehaviour

		public void Awake()
		{
			rb = NPC.Body.Rigidbody;
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (positionStuckDuration > 0f)
			{
				Gizmos.color = Color.red;
				var position = transform.position;
				Gizmos.DrawLine(position, position + Vector3.up * PositionStuckDistance);
				Gizmos.DrawLine(position, position + -Vector3.up * PositionStuckDistance);
			}
			
			if (movementTarget == null)
				return;

			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(movementTarget.Value, 0.1f);
		}
#endif

		public void Update()
		{
			if (lastPositionStuck == 0f || Time.time < lastPositionStuck + PositionStuckRecalculateAfter)
				return;
			
			if (NPC.Agent.HasPath)
				NPC.Agent.SetDestination(NPC.Agent.Destination);

			lastPositionStuck = 0f;
		}

		public void FixedUpdate()
		{
			var position = rb.position;
			var rotation = rb.rotation;
			
			var distanceToCeiling = GetCeilingDistance(position);
			var distanceToFloor = GetFloorDistance(position);
			
			Hover(position, distanceToCeiling, distanceToFloor);

			if (NPC.Paralyzed)
			{
				rb.AddTorque(new Vector3(Random.Range(-5, 5), Random.Range(-5, 5), Random.Range(-5, 5)), ForceMode.Impulse);
				return;
			}
			
			processAngleStuck();
			processPositionStuck();
			
			var aiMode = NPC.AIMode;
			if (aiMode == EAIMode.Walking)
			{
				if (NPC.Agent.HasPath)
					movementTarget = NPC.Agent.Agent.NextNode;

				if (movementTarget == null)
					return;
#if UNITY_EDITOR
				Debug.DrawLine(position, movementTarget.Value, new Color(0.25f, 0.5f, 0.75f));
#endif
				FlyTowards(movementTarget.Value);
				RotateTowards(movementTarget.Value);
			}
			else
			{
				var rotating = false;
				
				if (aiMode == EAIMode.Action)
				{
					var attackTarget = NPC.AttackTargetTransform;
					var otherTarget = NPC.OtherTargetTransform;

					if (attackTarget.NotNull())
					{
						RotateTowards(attackTarget.position);
						rotating = true;
					}
					else if (otherTarget.NotNull())
					{
						RotateTowards(otherTarget.position);
						rotating = true;
					}
				}

				if (!rotating)
					Stabilize(rotation);
			}
		}
		
		#endregion

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

				var data = (NPCData)NPC.Data;
				
				// Hover within range that can still attack, spot and sense targets
				var hoverRange = UseSpellRange ? Mathf.Min(NPC.SpellRange, data.SpotRange, data.SenseRange) : Mathf.Min(data.SpotRange, data.SenseRange);
				
				if (Mathf.Approximately(distanceToCeiling, float.MaxValue))
				{
					// No ceiling found, try to be within ground range
					if (distanceToFloor > hoverRange && NPC.AIMode != EAIMode.Walking)
						flightTarget.y -= distanceToFloor - hoverRange;
				}
				else if (Mathf.Approximately(distanceToFloor, float.MaxValue))
				{
					// No floor found, try to be within ceiling range
					if (distanceToCeiling > hoverRange && NPC.AIMode != EAIMode.Walking)
						flightTarget.y += distanceToCeiling - hoverRange;
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
			var speed = FlightSpeed - (FlightSpeed * NPC.SlowAmount);
			if (speed == 0f)
				return;
			
			rb.AddForce((target - rb.position).normalized * speed, ForceMode.Acceleration);
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

		#region Utility

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

		#endregion

		#region Anti Stuck

		private void processAngleStuck()
		{
			if (!AngleAntiStuckEnabled)
				return;
			
			var euler = rb.rotation.eulerAngles;
			
			var x = euler.x;
			var z = euler.z;

			if (x > 180f)
				x -= 360f;
			
			if (z > 180f)
				z -= 360f;

			if (Mathf.Abs(x) < AngleStuckDegree && Mathf.Abs(z) < AngleStuckDegree)
			{
				angleStuckDuration = 0f;
				return;
			}

			angleStuckDuration += Time.deltaTime;
			
			if (angleStuckDuration < AngleStuckTime)
				return;
			
			angleStuckDuration = 0f;
			
			Debug.LogWarning($"[Flight {gameObject.name}] Angle stuck, attempting to break out");
			
			rb.AddRelativeTorque(new Vector3(Random.Range(0f, 5f), Random.Range(0f, 5f), Random.Range(0f, 5f)) * 10f, ForceMode.VelocityChange);
			NPC.Chase.ResetChaseRange(true);
		}

		private void processPositionStuck()
		{
			if (NPC.AIMode != EAIMode.Walking)
				return;

			if (!PositionRaycastAntiStuckEnabled && !PositionVelocityAntiStuckEnabled)
				return;
			
			var velocityStuck = false;
			var raycastStuck = false;

			if (PositionVelocityAntiStuckEnabled && rb.linearVelocity.magnitude < PositionStuckVelocity) 
				velocityStuck = true;
			
			var position = rb.position;

			if (PositionRaycastAntiStuckEnabled && GetCeilingDistance(position) < PositionStuckDistance || GetFloorDistance(position) < PositionStuckDistance)
				raycastStuck = true;
			
			if (!velocityStuck && !raycastStuck)
			{
				positionStuckDuration = 0f;
				return;
			}

			positionStuckDuration += Time.deltaTime;
			
			if (positionStuckDuration < PositionStuckTime)
				return;
			
			positionStuckDuration = 0f;
			lastPositionStuck = Time.time;

			Debug.LogWarning($"[Flight {gameObject.name}] Position stuck, attempting to break out");
			
			rb.AddRelativeTorque(new Vector3(Random.Range(0f, 5f), Random.Range(0f, 5f), Random.Range(0f, 5f)) * 10f, ForceMode.VelocityChange);
			NPC.Chase.ResetChaseRange(true);
		}
		
		#endregion
	}
}