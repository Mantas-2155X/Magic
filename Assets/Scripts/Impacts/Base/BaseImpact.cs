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
			Pool();
		}
		
		public void Spawn(IProjectile source, Vector3 position, Vector3 angles)
		{
			Source = source;

			var tr = transform;
			tr.SetParent(World.World.Instance.Impacts);
			tr.position = position;
			tr.eulerAngles = angles;
			
			gameObject.SetActive(true);
		}

		public void Pool()
		{
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}