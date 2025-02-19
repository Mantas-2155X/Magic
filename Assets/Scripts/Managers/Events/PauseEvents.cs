using System;
using UnityEngine.Events;

namespace Managers.Events
{
	[Serializable]
	public class OnPausedEvent : UnityEvent { }
	
	[Serializable]
	public class OnUnpausedEvent : UnityEvent { }
}