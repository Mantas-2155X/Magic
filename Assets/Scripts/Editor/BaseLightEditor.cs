using Objects.Base;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(BaseLight), true), CanEditMultipleObjects]
	public class BaseLightEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Find Reflection Probes"))
			{
				var probes = FindObjectsByType<ReflectionProbe>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				
				var lights = targets;
				for (var i = 0; i < lights.Length; i++)
				{
					var light = (BaseLight)lights[i];
					var lightPos = light.transform.position;

					for (var k = 0; k < probes.Length; k++)
					{
						var probe = probes[k];
						var probePos = probe.transform.position + probe.center;
						
						var direction = (probePos - lightPos).normalized;
						var closestPoint = direction * (light.Light.range / 2f) + lightPos;
						
						if (probe.bounds.Contains(closestPoint))
							light.UpdateProbes.AddUnique(probe);
					}
					
					EditorUtility.SetDirty(light);
				}
			}

			if (GUILayout.Button("Clear Reflection Probes"))
			{
				var lights = targets;
				for (var i = 0; i < lights.Length; i++)
				{
					var light = (BaseLight)lights[i];
					light.UpdateProbes.Clear();
					
					EditorUtility.SetDirty(light);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}