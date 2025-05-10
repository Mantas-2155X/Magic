using System;
using UnityEngine.Events;

namespace Managers.Events
{
	[Serializable]
	public class OnPreSceneLoadEvent : UnityEvent<string> { }
}