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

			lmask = LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Broken", "Movement", "AfterPostProcessing");
			return lmask!.Value;
		}
		
		public static LayerMask GetMaskWithAlives()
		{
			if (lmaskWithAlives != null)
				return lmaskWithAlives.Value;

			lmaskWithAlives = GetMask() + LayerMask.GetMask("NPC", "Player");
			return lmaskWithAlives!.Value;
		}
		
		public static LayerMask GetMaskWithPlayer()
		{
			if (lmaskWithPlayer != null)
				return lmaskWithPlayer.Value;

			lmaskWithPlayer = GetMask() + LayerMask.GetMask("Player");
			return lmaskWithPlayer!.Value;
		}
		
		public static LayerMask GetMaskWithNPC()
		{
			if (lmaskWithNPC != null)
				return lmaskWithNPC.Value;

			lmaskWithNPC = GetMask() + LayerMask.GetMask("NPC");
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