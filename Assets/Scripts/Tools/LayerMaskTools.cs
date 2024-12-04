using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		private static LayerMask? mask1;
		public static LayerMask Mask1
		{
			get
			{
				if (mask1 != null)
					return mask1.Value;
				
				mask1 = LayerMask.GetMask("Player", "Ignore Raycast", "Walkthrough", "Projectile");
				return mask1!.Value;
			}
		}
		
		private static LayerMask? mask2;
		public static LayerMask Mask2
		{
			get
			{
				if (mask2 != null)
					return mask2.Value;
				
				mask2 = LayerMask.GetMask("Ignore Raycast", "Walkthrough", "Projectile");
				return mask2!.Value;
			}
		}
		
		public static bool ContainsLayer(this LayerMask mask, int layer)
		{
			return ContainsLayer(mask.value, layer);
		}
		
		public static bool ContainsLayer(this int mask, int layer)
		{
			return (mask & 1 << layer) != 0;
		}
	}
}