using UnityEngine;
using UnityEngine.Events;

namespace Tools
{
	public static class EventTools
	{
		public static void DrawListeners(Transform source, UnityEventBase unityEvent, Color color)
		{
			Gizmos.color = color;

			var pos = source.position;
			var listeners = unityEvent.GetPersistentEventCount();
				
			for (var i = 0; i < listeners; i++)
			{
				var target = unityEvent.GetPersistentTarget(i);
				if (target == null)
					continue;

				switch (target)
				{
					case Component component:
						Gizmos.DrawLine(pos, component.transform.position);
						break;
					case GameObject gameObject:
						Gizmos.DrawLine(pos, gameObject.transform.position);
						break;
				}
			}
		}
	}
}