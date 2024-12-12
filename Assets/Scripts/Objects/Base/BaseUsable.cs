using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Enums;
using Objects.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BaseUsable : MonoBehaviour, IUsable
	{
		[field: SerializeField]
		public virtual float UsableAfter { get; private set; }
		[field: SerializeField]
		public virtual EDestroyType DestroyAfterUse { get; private set; }

		private bool destroyed;
		private bool usable;

		public virtual void OnEnable()
		{
			if (UsableAfter == 0f)
				usable = true;
			else
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
			
			switch (DestroyAfterUse)
			{
				case EDestroyType.None:
					return true;
				case EDestroyType.GameObject:
					destroyed = true;
					Destroy(gameObject);
					break;
				case EDestroyType.Component:
					destroyed = true;
					Destroy(this);
					break;
			}
			
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