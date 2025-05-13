using System;
using State.Interfaces;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public static class IIDentifiableEditor
	{
		[MenuItem("CONTEXT/Object/Generate ObjectID")]
		public static void GenerateObjectID(MenuCommand command) 
		{
			if (command.context is not IIdentifiable identifiable || identifiable.IsNull())
				return;
			
			identifiable.ObjectID = Guid.NewGuid().ToString();
			EditorUtility.SetDirty((Component)identifiable);
		}
		
		[MenuItem("CONTEXT/Object/Generate Selected ObjectIDs")]
		public static void GenerateSelectedObjectIDs(MenuCommand command) 
		{
			var gameObjects = Selection.gameObjects;
			for (var i = 0; i < gameObjects.Length; i++)
			{
				var gameObject = gameObjects[i];
				
				var identifiables = gameObject.GetComponents<IIdentifiable>();
				for (var k = 0; k < identifiables.Length; k++)
				{
					var identifiable = identifiables[k];
					identifiable.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty((Component)identifiable);
				}
			}
		}
	}
}