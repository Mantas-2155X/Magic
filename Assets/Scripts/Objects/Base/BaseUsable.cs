using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseUsable : MonoBehaviour, IUsable
	{
		[field: SerializeField]
		public virtual float UsableAfter { get; private set; }
		[field: SerializeField]
		public virtual bool DestroyAfterUse { get; private set; }

		private bool destroyed;
		private bool usable;

		public void Awake()
		{
			setUsable().Forget();
		}

		public virtual bool CanUse(IAlive user)
		{
			return !destroyed && usable && user.IsAlive;
		}
		
		public virtual bool Use(IAlive user)
		{
			if (!CanUse(user))
				return false;
			
			if (!DestroyAfterUse)
				return true;
			
			destroyed = true;
			Destroy(gameObject);

			return true;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}
		
		private async UniTask setUsable()
		{
			await UniTask.WaitForSeconds(UsableAfter);
			
			usable = true;
		}
	}
}