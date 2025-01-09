using System;
using AI.Interfaces;
using UnityEngine.Events;

namespace Objects.Events
{
	[Serializable]
	public class OnDoorOpenedEvent : UnityEvent { }
	
	[Serializable]
	public class OnDoorOpeningEvent : UnityEvent { }
	
	[Serializable]
	public class OnDoorClosedEvent : UnityEvent { }
	
	[Serializable]
	public class OnDoorClosingEvent : UnityEvent { }
}