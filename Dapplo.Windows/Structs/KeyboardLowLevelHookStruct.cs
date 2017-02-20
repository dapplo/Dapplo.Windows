//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	///     Contains information about a low-level keyboard input event.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct KeyboardLowLevelHookStruct
	{
		/// <summary>
		///     A virtual-key code. The code must be a value in the range 1 to 254.
		/// </summary>
		public VirtualKeyCodes VirtualKeyCode;

		/// <summary>
		///     A hardware scan code for the key.
		/// </summary>
		public ScanCodes ScanCode;

		/// <summary>
		///     The extended-key flag, event-injected flags, context code, and transition-state flag.
		///     This member is specified as follows. An application can use the following values to test the keystroke flags.
		///     Testing LLKHF_INJECTED (bit 4) will tell you whether the event was injected.
		///     If it was, then testing LLKHF_LOWER_IL_INJECTED (bit 1) will tell you whether or not the event
		///     was injected from a process running at lower integrity level.
		/// </summary>
		public ExtendedKeyFlags Flags;

		/// <summary>
		///     The time stamp for this message, equivalent to what GetMessageTime would return for this message.
		/// </summary>
		public uint TimeStamp;

		/// <summary>
		///     Additional information associated with the message.
		/// </summary>
		public UIntPtr dwExtraInfo;
	}
}