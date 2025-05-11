using ScriptableObjects;
using State.Interfaces;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Combat.Decals.Interfaces
{
	public interface IDecal : IIdentifiable
	{
		public DecalData DecalData { get; }
		
		public DecalProjector Projector { get; }
		
		public void Spawn(Vector3 position, Quaternion angles, Transform attach);
	}
}