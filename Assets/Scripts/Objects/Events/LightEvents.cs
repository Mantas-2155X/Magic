using System;
using UnityEngine.Events;

namespace Objects.Events
{
	[Serializable]
	public class OnLightEnabledEvent : UnityEvent { }
	
	[Serializable]
	public class OnLightDisabledEvent : UnityEvent { }
}