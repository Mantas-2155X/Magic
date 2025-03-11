using UnityEngine;

namespace AI.PathFinding.Structs
{
	public struct SRaycastHit
	{
		internal Vector3 m_Point;
		
		internal Vector3 m_Normal;
		
		internal uint m_FaceID;
		
		internal float m_Distance;
		
		internal Vector2 m_UV;
		
		public int m_Collider;
	}
}