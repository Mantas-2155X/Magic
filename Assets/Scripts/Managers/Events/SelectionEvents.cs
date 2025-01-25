using System;
using UnityEngine;
using UnityEngine.Events;

namespace Managers.Events
{
	[Serializable]
	public class OnSelectionChangedEvent : UnityEvent<GameObject, GameObject> { }
}