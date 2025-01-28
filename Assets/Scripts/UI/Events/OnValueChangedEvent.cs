using System;
using UnityEngine.Events;

namespace UI.Events
{
	[Serializable]
	public class OnValueChangedEvent : UnityEvent<float> { }
}