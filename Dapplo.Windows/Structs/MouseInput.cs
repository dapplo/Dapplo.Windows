#region Dapplo 2016 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Dapplo.Windows
// 
// Dapplo.Windows is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Dapplo.Windows is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Enums;

#endregion

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// Contains information about a simulated mouse event.
	/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646273(v=vs.85).aspx">MOUSEINPUT structure</a>
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct MouseInput
	{
		/// <summary>
		/// The absolute position of the mouse, or the amount of motion since the last mouse event was generated,
		/// depending on the value of the dwFlags member.
		/// Absolute data is specified as the x coordinate of the mouse;
		/// relative data is specified as the number of pixels moved.
		/// </summary>
		int dx;
		/// <summary>
		/// The absolute position of the mouse, or the amount of motion since the last mouse event was generated,
		/// depending on the value of the dwFlags member.
		/// Absolute data is specified as the y coordinate of the mouse;
		/// relative data is specified as the number of pixels moved.
		/// </summary>
		int dy;

		/// <summary>
		/// If dwFlags contains MOUSEEVENTF_WHEEL, then mouseData specifies the amount of wheel movement.
		/// A positive value indicates that the wheel was rotated forward, away from the user;
		/// a negative value indicates that the wheel was rotated backward, toward the user.
		/// One wheel click is defined as WHEEL_DELTA, which is 120.
		/// 
		/// Windows Vista: If dwFlags contains MOUSEEVENTF_HWHEEL, then dwData specifies the amount of wheel movement.
		/// A positive value indicates that the wheel was rotated to the right;
		/// a negative value indicates that the wheel was rotated to the left.
		/// One wheel click is defined as WHEEL_DELTA, which is 120.
		/// 
		/// If dwFlags does not contain MOUSEEVENTF_WHEEL, MOUSEEVENTF_XDOWN, or MOUSEEVENTF_XUP, then mouseData should be zero.
		/// If dwFlags contains MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP, then mouseData specifies which X buttons were pressed or released.
		/// This value may be any combination of the following flags.
		/// </summary>
		int MouseData;

		/// <summary>
		/// A set of bit flags that specify various aspects of mouse motion and button clicks.
		/// The bits in this member can be any reasonable combination of the following values.
		/// </summary>
		MouseEventFlags MouseEventFlags;
		/// <summary>
		/// The time stamp for the event, in milliseconds. If this parameter is 0, the system will provide its own time stamp.
		/// </summary>
		uint Timestamp;
		/// <summary>
		/// An additional value associated with the mouse event. An application calls GetMessageExtraInfo to obtain this extra information.
		/// </summary>
		UIntPtr dwExtraInfo;

		/// <summary>
		/// Create a MouseInput struct for a wheel move
		/// </summary>
		/// <param name="wheelDelta">How much does the wheel move</param>
		/// <param name="timestamp">The time stamp for the event</param>
		/// <returns>MouseInput</returns>
		public static MouseInput MoveMouseWheel(int wheelDelta, int timestamp = 0)
		{
			return new MouseInput
			{
				MouseData = wheelDelta,
				MouseEventFlags = MouseEventFlags.Wheel
			};
		}
	}
}