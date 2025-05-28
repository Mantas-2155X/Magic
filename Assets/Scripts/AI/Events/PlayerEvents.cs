using System;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AI.Events
{
	[Serializable]
	public class OnScrollEvent : UnityEvent<Player, InputDevice, float> {}
}