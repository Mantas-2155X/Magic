using UnityEngine;

namespace Tools
{
	public static class TextureTools
	{
		public static Texture2D Resize(Texture2D texture2D, int targetX, int targetY)
		{
			var prevRt = RenderTexture.active;
			
			var rt = new RenderTexture(targetX, targetY, 24);
			RenderTexture.active = rt;
			
			Graphics.Blit(texture2D, rt);
			
			var result = new Texture2D(targetX, targetY, TextureFormat.RGBA32, false);
			result.ReadPixels(new Rect(0, 0, targetX, targetY), 0, 0);
			result.Apply();

			RenderTexture.active = prevRt;
			return result;
		}
	}
}