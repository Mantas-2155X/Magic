using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace AI.PathFinding
{
	[ExecuteInEditMode]
	public class Area : MonoBehaviour
	{
		[Header("Area Settings")]
		[SerializeField]
		public Vector3 Offset = Vector3.zero;
		[SerializeField]
		public Vector3 Size = Vector3.one;
		
		[SerializeField]
		public float Cost;
		
		[Header("Draw")]
		[SerializeField]
		public bool DrawBounds;
		
		public static readonly List<Area> Areas = new ();

		private Transform thisTr;

		#region MonoBehaviour

		public void Awake()
		{
			thisTr = transform;
		}

		public void OnEnable()
		{
			Areas.Add(this);
		}

		public void OnDisable()
		{
			Areas.Remove(this);
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			if (!DrawBounds)
				return;
			
			Gizmos.DrawWireCube(transform.position + Offset, Size);
		}
#endif
		
		#endregion

		#region API

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 GetPosition()
		{
			var trPos = thisTr.position;
			
			float3 position;
			position.x = trPos.x + Offset.x;
			position.y = trPos.y + Offset.y;
			position.z = trPos.z + Offset.z;
			
			return position;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float3 GetHalfSize()
		{
			float3 size;
			size.x = Size.x / 2f;
			size.y = Size.y / 2f;
			size.z = Size.z / 2f;
			
			return size;
		}

		#endregion
	}
}