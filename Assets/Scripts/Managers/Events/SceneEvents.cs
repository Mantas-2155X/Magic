using System;
using ScriptableObjects;
using UnityEngine.Events;

namespace Managers.Events
{
	[Serializable]
	public class OnPreSceneLoadEvent : UnityEvent<SceneData> { }
	
	[Serializable]
	public class OnPostSceneLoadEvent : UnityEvent<SceneData> { }
}