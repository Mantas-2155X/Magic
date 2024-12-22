using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		private static LayerMask? lmask;
		private static LayerMask? lmaskWithPlayer;

		public static LayerMask GetMask()
		{
			if (lmask != null)
				return lmask.Value;

			lmask = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile");
			return lmask!.Value;
		}
		
		public static LayerMask GetMaskWithPlayer()
		{
			if (lmaskWithPlayer != null)
				return lmaskWithPlayer.Value;

			lmaskWithPlayer = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Player");
			return lmaskWithPlayer!.Value;
		}
		
		public static bool ContainsLayer(this int mask, int layer)
		{
			return (mask & 1 << layer) != 0;
		}
	}
}