using System;

namespace ScriptableObjects.Enums
{
	[Flags]
	public enum ETag
	{
		None = 0,
		RestoresHealth = 1,
		RestoresMana = 2,
		Damage = 4,
		CrowdControl = 8,
		Defense = 16,
		RestoresEnergy = 32,
	}
}