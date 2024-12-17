using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Tools
{
	public class ThumbnailGenerator : MonoBehaviour
	{
		[SerializeField]
		public Camera Camera;

		public void Take()
		{
			takeDelayed().Forget();
		}

		private async UniTaskVoid takeDelayed()
		{
			await UniTask.WaitForEndOfFrame();
			
			var rt = RenderTexture.active;
			var texture = new Texture2D(Screen.height, Screen.height, TextureFormat.RGB24, false);
			
			Camera.Render();
			
			texture.ReadPixels(new Rect(0, 0, Screen.height, Screen.height), 0, 0);
			texture.Apply();

			RenderTexture.active = rt;
			
			var bytes = texture.EncodeToPNG();
			
			DestroyImmediate(texture);
			
			await File.WriteAllBytesAsync("image.png", bytes);
		}
	}
}