using System;

namespace AI.PathFinding.Enums
{
	[Flags]
	public enum ENodeAvailabilityFlags
	{
		None = 0,
		Available = 1,
		InsideObject = 2,
		NoConnections = 4
	}
}