using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		private static LayerMask? lmask;
		private static LayerMask? lmaskWithAlives;
		private static LayerMask? lmaskWithPlayer;
		private static LayerMask? lmaskWithPlayerAndWater;

		public static LayerMask GetMask()
		{
			if (lmask != null)
				return lmask.Value;

			lmask = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile");
			return lmask!.Value;
		}
		
		public static LayerMask GetMaskWithAlives()
		{
			if (lmaskWithAlives != null)
				return lmaskWithAlives.Value;

			lmaskWithAlives = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "NPC", "Player");
			return lmaskWithAlives!.Value;
		}
		
		public static LayerMask GetMaskWithPlayer()
		{
			if (lmaskWithPlayer != null)
				return lmaskWithPlayer.Value;

			lmaskWithPlayer = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Player");
			return lmaskWithPlayer!.Value;
		}
		
		public static LayerMask GetMaskWithPlayerAndWater()
		{
			if (lmaskWithPlayerAndWater != null)
				return lmaskWithPlayerAndWater.Value;

			lmaskWithPlayerAndWater = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Player", "Water");
			return lmaskWithPlayerAndWater!.Value;
		}
	}
}