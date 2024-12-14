using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
		public static LayerMask GetMask() => LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile");
		public static LayerMask GetMaskWithPlayer() => LayerMask.GetMask("TransparentFX", "Ignore Raycast", "UI", "Projectile", "Player");
		
		public static bool ContainsLayer(this int mask, int layer)
		{
			return (mask & 1 << layer) != 0;
		}
	}
}