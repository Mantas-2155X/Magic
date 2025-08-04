using System;
using UnityEngine.Events;

namespace Objects.Events
{
	[Serializable]
	public class OnElevatorElevatedEvent : UnityEvent { }
	
	[Serializable]
	public class OnElevatorElevatingEvent : UnityEvent { }
	
	[Serializable]
	public class OnElevatorLoweredEvent : UnityEvent { }
	
	[Serializable]
	public class OnElevatorLoweringEvent : UnityEvent { }
}