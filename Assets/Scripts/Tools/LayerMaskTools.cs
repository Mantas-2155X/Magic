using UnityEngine;

namespace Tools
{
	public static class LayerMaskTools
	{
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