using System;

namespace Dapplo.Windows.Enums
{
	[Flags]
	public enum KeyEventFlags : uint
	{
		EXTENDEDKEY = 0x0001,
		KEYUP = 0x0002,
		SCANCODE = 0x0008,
		UNICODE = 0x0004
	}
}