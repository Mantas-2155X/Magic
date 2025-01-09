using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Tools
{
	public static class NavMeshTools
	{
		public static bool IsJumpLink(OffMeshLinkData data)
		{
			if (!data.valid || data.owner is not NavMeshLink link)
				return false;
			
			return link.area == 2;
		}
		
		public static bool IsDoorLink(OffMeshLinkData data)
		{
			if (!data.valid || data.owner is not NavMeshLink link)
				return false;
			
			return link.area == 3;
		}
	}
}