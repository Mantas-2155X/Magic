using Combat.Enums;
using Tools;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Managers
{
	public class CursorManager
	{
		private static CursorManager instance;
		public static CursorManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new CursorManager();
				return instance;
			}
		}

		public EElement Element { get; private set; }
		public int Size { get; private set; }

		public void SetElement(EElement element)
		{
			if (Element == element)
				return;
			
			Element = element;
			updateCursor();
		}
		
		public void SetSize(int size)
		{
			if (Size == size)
				return;
			
			Size = size;
			updateCursor();
		}

		private void updateCursor()
		{
			if (Element == EElement.Unknown || Size <= 0)
				return;
			
			var cursor = Addressables.LoadAssetAsync<Texture2D>($"Assets/Textures/Cursor/{Element}.png").WaitForCompletion();
			var resized = TextureTools.Resize(cursor, Size, Size);
			
			Cursor.SetCursor(resized, new Vector2(Size / 2f, Size / 2f), CursorMode.Auto);
			Addressables.Release(cursor);
		}
	}
}