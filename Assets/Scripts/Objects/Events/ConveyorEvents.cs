using System;
using UnityEngine.Events;

namespace Objects.Events
{
	[Serializable]
	public class OnConveyorRunningEvent : UnityEvent { }
	
	[Serializable]
	public class OnConveyorAcceleratingEvent : UnityEvent { }
	
	[Serializable]
	public class OnConveyorStoppedEvent : UnityEvent { }
	
	[Serializable]
	public class OnConveyorDeceleratingEvent : UnityEvent { }
}