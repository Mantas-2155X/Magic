using System;
using UnityEngine.Events;

namespace Managers.Events
{
	[Serializable]
	public class OnConsoleEntryAddedEvent : UnityEvent<ConsoleManager.SConsoleEntry> { }

	[Serializable]
	public class OnConsoleClearedEvent : UnityEvent { }
}