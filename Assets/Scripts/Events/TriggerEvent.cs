using System;
using State.Interfaces;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{
	[Serializable]
	public class OnTriggerEvent : UnityEvent<IIdentifiable> { }

}