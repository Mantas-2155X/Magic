using System;

namespace UI.Enums
{
	[Flags]
	public enum ENoticePresetFlags
	{
		None = 0,
		Flashlight = 1,
		Interact = 2,
		Grab = 4,
		Resource = 8
	}
}