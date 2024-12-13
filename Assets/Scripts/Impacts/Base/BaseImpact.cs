using Impacts.Interfaces;
using Managers;
using Projectiles.Interfaces;
using UnityEngine;

namespace Impacts.Base
{
	public class BaseImpact : MonoBehaviour, IImpact
	{
		public IProjectile Source { get; private set; }
		
		public void OnDisable()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		public void Spawn(IProjectile source, Vector3 position, Quaternion angles, bool parent)
		{
			Source = source;

			var tr = transform;
			
			if (parent)
				tr.SetParent(World.World.Instance.Impacts);
			
			tr.position = position;
			tr.rotation = angles;
			
			gameObject.SetActive(true);
		}

		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}