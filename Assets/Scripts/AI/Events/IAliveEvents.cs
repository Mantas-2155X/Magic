using System;
using AI.Interfaces;
using Combat.Enums;
using UnityEngine.Events;

namespace AI.Events
{
	[Serializable]
	public class OnRestoreHealthEvent : UnityEvent<IAlive, float, object> { }

	[Serializable]
	public class OnDamageEvent : UnityEvent<IAlive, float, object, EElement> { }
	
	[Serializable]
	public class OnRestoreManaEvent : UnityEvent<IAlive, float, object> { }
	
	[Serializable]
	public class OnTakeManaEvent : UnityEvent<IAlive, float, object> { }

	[Serializable]
	public class OnDeathEvent : UnityEvent<IAlive, object> { }
	
	[Serializable]
	public class OnSpawnEvent : UnityEvent<IAlive> { }
	
	[Serializable]
	public class OnRelationshipGroupChangedEvent : UnityEvent<IAlive, int, int> { }
}