using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		private static LayerMask? lmask;
		private static LayerMask? lmaskWithPlayer;
		private static LayerMask? lmaskAlives;

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
		
		public static LayerMask GetMaskAlives()
		{
			if (lmaskAlives != null)
				return lmaskAlives.Value;

			lmaskAlives = LayerMask.GetMask("NPC", "Player");
			return lmaskAlives!.Value;
		}
		
		public static bool ContainsLayer(this int mask, int layer)
		{
			return (mask & 1 << layer) != 0;
		}
	}
}