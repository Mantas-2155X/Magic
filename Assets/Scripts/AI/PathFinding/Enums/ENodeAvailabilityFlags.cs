using System;

namespace AI.PathFinding.Enums
{
	[Flags]
	public enum ENodeAvailabilityFlags
	{
		Available = 0,
		InsideObject = 1,
		Obstructed = 2
	}
}