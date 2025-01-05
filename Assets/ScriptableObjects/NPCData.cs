using UnityEngine;
using UnityEngine.Serialization;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class NPCData : AliveData
	{
		/// <summary>
		/// Distance around itself that the npc can sense targets in
		/// (WithinRange)
		/// </summary>
		[Header("AI")]
		[SerializeField]
		public float SenseRange = 25f;

		/// <summary>
		/// Distance between the npc and the patrol point at which the npc is considered to have reached the point
		/// (Patrol)
		/// </summary>
		[SerializeField]
		public float PatrolReachRange = 0.5f;
		
		/// <summary>
		/// How fast can the npc rotate when performing an action
		/// (AimAt)
		/// </summary>
		[SerializeField]
		public float RotationSpeed = 480f;

		/// <summary>
		/// Maximum look angle between the npc and the target which the npc deems close enough
		/// (AimAt)
		/// </summary>
		[SerializeField]
		public float AimAngle = 5f;

		/// <summary>
		/// Wander every x seconds after the last walking state finished
		/// (Wander)
		/// </summary>
		[SerializeField]
		public float WanderEvery = 1f;
		
		/// <summary>
		/// How far around the npc communications are received and sent
		/// </summary>
		[SerializeField]
		public float CommunicateRange = 5f;

		/// <summary>
		/// Multiplying starting value of a resource with this indicates the point when it is considered as a low resource 
		/// </summary>
		[SerializeField]
		public float LowResourcesMultiplier = 0.15f;

		/// <summary>
		/// Multiply cooldown time with variation to get maximum time of how long the cooldown is extended to add variance
		/// </summary>
		public float SpellCooldownVariation = 0.15f;
		
		/// <summary>
		/// Maximum time to wait before casting after switching spell
		/// </summary>
		public float SpellSwitchCastCooldown = 0.25f;
		
		/// <summary>
		/// Wait for x second after the last usesomething state finished
		/// </summary>
		[SerializeField]
		public float UseResourceEvery = 1f;
	}
}