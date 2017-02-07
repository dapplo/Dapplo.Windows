using System;

namespace Dapplo.Windows.Enums
{
	/// <summary>
	/// A set of bit flags that specify various aspects of mouse motion and button clicks.
	/// The bits in this member can be any reasonable combination of the following values.
	/// The bit flags that specify mouse button status are set to indicate changes in status, not ongoing conditions.
	/// For example, if the left mouse button is pressed and held down,
	/// MOUSEEVENTF_LEFTDOWN is set when the left button is first pressed, but not for subsequent motions.
	/// Similarly, MOUSEEVENTF_LEFTUP is set only when the button is first released.
	/// You cannot specify both the MOUSEEVENTF_WHEEL flag and either MOUSEEVENTF_XDOWN or MOUSEEVENTF_XUP flags simultaneously
	/// in the dwFlags parameter, because they both require use of the mouseData field.
	/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646273(v=vs.85).aspx"></a>
	/// </summary>
	[Flags]
	public enum MouseEventFlags : uint
	{
		/// <summary>
		/// Movement occurred.
		/// </summary>
		Move = 0x0001,
		/// <summary>
		/// The left button was pressed.
		/// </summary>
		LeftDown = 0x0002,
		/// <summary>
		/// The left button was released.
		/// </summary>
		LeftUp = 0x0004,
		/// <summary>
		/// The right button was pressed.
		/// </summary>
		RightDown = 0x0008,
		/// <summary>
		/// The right button was released.
		/// </summary>
		RightUp = 0x0010,
		/// <summary>
		/// The middle button was pressed.
		/// </summary>
		MiddleDown = 0x0020,
		/// <summary>
		/// The middle button was released.
		/// </summary>
		MiddleUp = 0x0040,
		/// <summary>
		/// An X button was pressed.
		/// </summary>
		XDown = 0x0080,
		/// <summary>
		/// An X button was released.
		/// </summary>
		XUp = 0x0100,
		/// <summary>
		/// The wheel was moved, if the mouse has a wheel. The amount of movement is specified in mouseData.
		/// </summary>
		Wheel = 0x0800,
		/// <summary>
		/// The wheel was moved horizontally, if the mouse has a wheel.
		/// The amount of movement is specified in mouseData.
		/// Windows XP/2000:  This value is not supported.
		/// </summary>
		HorizontalWheel = 0x01000,
		/// <summary>
		/// The WM_MOUSEMOVE messages will not be coalesced. The default behavior is to coalesce WM_MOUSEMOVE messages.
		/// Windows XP/2000:  This value is not supported.
		/// </summary>
		MoveNocoalesce = 0x2000,
		/// <summary>
		/// Maps coordinates to the entire desktop. Must be used with MOUSEEVENTF_ABSOLUTE.
		/// </summary>
		Virtualdesk = 0x4000,
		/// <summary>
		/// The dx and dy members contain normalized absolute coordinates.
		/// If the flag is not set, dxand dy contain relative data (the change in position since the last reported position).
		/// This flag can be set, or not set, regardless of what kind of mouse or other pointing device, if any, is connected to the system.
		/// For further information about relative mouse motion, see the following Remarks section.
		/// </summary>
		Absolute = 0x8000
	}
}