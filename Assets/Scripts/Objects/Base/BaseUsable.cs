using System.Runtime.CompilerServices;
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
		public virtual string DisplayName { get; set; }

		[field: SerializeField]
		public virtual float UsableAfter { get; private set; }
		[field: SerializeField]
		public virtual EDestroyType DestroyAfterUse { get; private set; }

		private bool destroyed;
		private bool usable;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public virtual void Awake()
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				init = true;
			}

			DisplayName = thisGo.name;
		}
		
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
					Destroy(thisGo);
					break;
				case EDestroyType.Component:
					destroyed = true;
					Destroy(this);
					break;
			}
			
			return true;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid setUsable()
		{
			await UniTask.WaitForSeconds(UsableAfter);
			
			usable = true;
		}
	}
}