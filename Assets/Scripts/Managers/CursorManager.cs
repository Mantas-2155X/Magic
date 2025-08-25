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
			Element = element;
			updateCursor();
		}
		
		public void SetSize(int size)
		{
			Size = size;
			updateCursor();
		}

		private void updateCursor()
		{
			if (Size <= 0)
				Size = 24;
			
			if (Element != EElement.Unknown)
			{
				var cursor = Addressables.LoadAssetAsync<Texture2D>($"Assets/Textures/Cursor/{Element}.png").WaitForCompletion();
				var resized = TextureTools.Resize(cursor, Size, Size);
				
				Cursor.SetCursor(resized, new Vector2(Size / 2f, Size / 2f), CursorMode.Auto);
			}
			else
			{
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
		}
	}
}