using System.Collections.Generic;

namespace Tools
{
	public static class ListTools
	{
		public static bool AddUnique<T>(this List<T> list, T item)
		{
			if (list.Contains(item))
				return false;
			
			list.Add(item);
			return true;
		}
	}
}