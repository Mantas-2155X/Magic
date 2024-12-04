using Impacts.Interfaces;
using Projectiles.Interfaces;
using UnityEngine;

namespace Impacts.Base
{
	public class BaseImpact : MonoBehaviour, IImpact
	{
		public IProjectile Source { get; private set; }
		
		public void Spawn(IProjectile source, Vector3 position, Vector3 angles)
		{
			Source = source;

			var tr = transform;
			tr.position = position;
			tr.eulerAngles = angles;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}