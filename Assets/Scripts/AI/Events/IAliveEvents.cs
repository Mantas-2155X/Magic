using System;
using AI.Interfaces;
using Combat.Enums;
using Combat.Spells.Interfaces;
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
	public class OnRestoreEnergyEvent : UnityEvent<IAlive, float, object> { }

	[Serializable]
	public class OnTakeManaEvent : UnityEvent<IAlive, float, object> { }

	[Serializable]
	public class OnTakeEnergyEvent : UnityEvent<IAlive, float, object> { }

	[Serializable]
	public class OnDeathEvent : UnityEvent<IAlive, object> { }
	
	[Serializable]
	public class OnSpawnEvent : UnityEvent<IAlive> { }
	
	[Serializable]
	public class OnRelationshipGroupChangedEvent : UnityEvent<IAlive, int, int> { }

	[Serializable]
	public class OnSpellSelectedEvent : UnityEvent<IAlive, ISpell, ISpell> {}
}