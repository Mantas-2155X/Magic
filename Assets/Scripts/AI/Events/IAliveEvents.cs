using System;
using AI.Interfaces;
using UnityEngine.Events;

namespace AI.Events
{
	[Serializable]
	public class OnHealEvent : UnityEvent<IAlive, int, object> { }

	[Serializable]
	public class OnDamageEvent : UnityEvent<IAlive, int, object> { }
	
	[Serializable]
	public class OnManaGenerateEvent : UnityEvent<IAlive, int, object> { }
	
	[Serializable]
	public class OnManaUseEvent : UnityEvent<IAlive, int, object> { }

	[Serializable]
	public class OnDeathEvent : UnityEvent<IAlive, object> { }
	
	[Serializable]
	public class OnSpawnEvent : UnityEvent<IAlive> { }
}