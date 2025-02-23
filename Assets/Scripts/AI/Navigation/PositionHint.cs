using System.Collections.Generic;
using UnityEngine;

namespace AI.Navigation
{
	public class PositionHint : MonoBehaviour
	{
		public static readonly Dictionary<PositionHint, Vector3> Hints = new ();

		[SerializeField]
		public PositionHint NextHint;
		
		public Vector3 Position { get; private set; }
		
		public void Awake()
		{
			Position = transform.position;
			Hints.Add(this, Position);
		}

		public void OnDestroy()
		{
			Hints.Remove(this);
		}
		
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.color = Color.magenta;
			Gizmos.DrawSphere(transform.position, 0.25f);
			
			if (NextHint != null)
				Gizmos.DrawLine(transform.position, NextHint.transform.position);
		}

		[UnityEditor.MenuItem("GameObject/AI/Position Hint")]
		public static void CreateHint(UnityEditor.MenuCommand menuCommand)
		{
			var hint = new GameObject("Position Hint");
			hint.AddComponent<PositionHint>();
			
			UnityEditor.GameObjectUtility.SetParentAndAlign(hint, menuCommand.context as GameObject);
			UnityEditor.Undo.RegisterCreatedObjectUndo(hint, "Create " + hint.name);
			UnityEditor.Selection.activeObject = hint;
		}
		
		[UnityEditor.MenuItem("GameObject/AI/Position Hint Pair")]
		public static void CreateHintPair(UnityEditor.MenuCommand menuCommand)
		{
			var parent = new GameObject("Position Hint Pair");

			var firstGo = new GameObject("Position Hint 1");
			var firstHint = firstGo.AddComponent<PositionHint>();

			var firstTr = firstGo.transform;
			firstTr.parent = parent.transform;
			firstTr.localPosition = new Vector3(0, 5f, 0);
			
			var secondGo = new GameObject("Position Hint 2");
			var secondHint = secondGo.AddComponent<PositionHint>();

			var secondTr = secondGo.transform;
			secondTr.parent = parent.transform;
			secondTr.localPosition = new Vector3(0, -5f, 0);

			firstHint.NextHint = secondHint;
			secondHint.NextHint = firstHint;
			
			UnityEditor.GameObjectUtility.SetParentAndAlign(parent, menuCommand.context as GameObject);
			UnityEditor.Undo.RegisterCreatedObjectUndo(parent, "Create " + parent.name);
			UnityEditor.Selection.activeObject = parent;
		}
#endif
	}
}