using System;

namespace ScriptableObjects.Enums
{
	[Flags]
	public enum ETag
	{
		None = 0,
		RestoresHealth = 1,
		RestoresMana = 2,
	}
}