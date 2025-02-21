using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		private static LayerMask? lmask;
		private static LayerMask? lmaskWithAlives;
		private static LayerMask? lmaskWithPlayer;
		private static LayerMask? lmaskWithNPC;
		private static LayerMask? lmaskPlayer;

		public static LayerMask GetMask()
		{
			if (lmask != null)
				return lmask.Value;

			lmask = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Broken");
			return lmask!.Value;
		}
		
		public static LayerMask GetMaskWithAlives()
		{
			if (lmaskWithAlives != null)
				return lmaskWithAlives.Value;

			lmaskWithAlives = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Broken", "NPC", "Player");
			return lmaskWithAlives!.Value;
		}
		
		public static LayerMask GetMaskWithPlayer()
		{
			if (lmaskWithPlayer != null)
				return lmaskWithPlayer.Value;

			lmaskWithPlayer = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Broken", "Player");
			return lmaskWithPlayer!.Value;
		}
		
		public static LayerMask GetMaskWithNPC()
		{
			if (lmaskWithNPC != null)
				return lmaskWithNPC.Value;

			lmaskWithNPC = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Broken", "NPC");
			return lmaskWithNPC!.Value;
		}
		
		public static LayerMask GetMaskPlayer()
		{
			if (lmaskPlayer != null)
				return lmaskPlayer.Value;

			lmaskPlayer = LayerMask.GetMask("Player");
			return lmaskPlayer!.Value;
		}
	}
}