using AI.Interfaces;
using Objects.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseUsable : MonoBehaviour, IUsable
	{
		public virtual bool DestroyAfterUse { get; private set; } = true;

		public virtual void Use(IAlive user)
		{
			if (!DestroyAfterUse)
				return;

			Destroy(gameObject);
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
	}
}