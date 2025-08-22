using ScriptableObjects;
using State.Interfaces;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Combat.Decals.Interfaces
{
	public interface IDecal : ISaveable
	{
		public DecalData DecalData { get; }
		
		public DecalProjector Projector { get; }
		public BoxCollider Collider { get; }
		
		public IIdentifiable Attach { get; }
		
		public float CreatedTime { get; }
		public float NormalizedTime { get; }

		public void Spawn(Vector3 position, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f);
	}
}