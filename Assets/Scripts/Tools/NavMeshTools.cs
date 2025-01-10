using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Tools
{
	public static class NavMeshTools
	{
		private static int? areaMask;

		public static int GetAreaMask()
		{
			if (areaMask != null)
				return areaMask.Value;
			
			areaMask = 0;
			areaMask += 1 << NavMesh.GetAreaFromName("Walkable");
			areaMask += 1 << NavMesh.GetAreaFromName("Jump");
			areaMask += 1 << NavMesh.GetAreaFromName("Elevator");
			
			return areaMask!.Value;
		}
		
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
		
		public static bool IsElevatorLink(OffMeshLinkData data)
		{
			if (!data.valid || data.owner is not NavMeshLink link)
				return false;
			
			return link.area == 4;
		}
		
		// https://discussions.unity.com/t/cost-of-a-navmeshpath/643664/12
		public static int IndexFromMask(int mask)
		{
			for (int i = 0; i < 32; ++i)
			{
				if ((1 << i & mask) != 0)
				{
					return i;
				}
			}
			return -1;
		}

		// https://discussions.unity.com/t/cost-of-a-navmeshpath/643664/12
		public static float Cost(this NavMeshPath path)
		{
			if (path.corners.Length < 2) return 0;

			float cost = 0;
			NavMeshHit hit;
			NavMesh.SamplePosition(path.corners[0], out hit, 0.1f, NavMesh.AllAreas);
			Vector3 rayStart = path.corners[0];
			int mask = hit.mask;
			int index = IndexFromMask(mask);

			for (int i = 1; i < path.corners.Length; ++i)
			{
				//The 100 is just a random value I chose
				for(int x = 0; x < 100; x++)
				{
					NavMesh.Raycast(rayStart, path.corners[i], out hit, mask);

					cost += NavMesh.GetAreaCost(index) * hit.distance;

					if (hit.mask != 0) mask = hit.mask;

					index = IndexFromMask(mask);
					rayStart = hit.position;

					if (hit.mask == 0)
					{ //hit boundary; move startPoint of ray a bit closer to endpoint
						rayStart += (path.corners[i] - rayStart).normalized * 0.01f;
					}

					if (!hit.hit) break;
				}
			}

			return cost;
		}
	}
}