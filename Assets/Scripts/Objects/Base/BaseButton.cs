using AI.Interfaces;
using Objects.Events;
using Tools;
using UnityEngine;

namespace Objects.Base
{
	public class BaseButton : BaseObject
	{
		[SerializeField]
		public OnButtonUsedEvent OnButtonUsedEvent = new ();
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			OnButtonUsedEvent?.Invoke();
			return true;
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnButtonUsedEvent, Color.blue);
		}
#endif
	}
}