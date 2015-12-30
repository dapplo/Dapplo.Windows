/*
 * dapplo - building blocks for desktop applications
 * Copyright (C) Dapplo 2015-2016
 * 
 * For more information see: http://dapplo.net/
 * dapplo repositories are hosted on GitHub: https://github.com/dapplo
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using Dapplo.Windows.Enums;
using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Structs
{
	/// <summary>
	/// Contains information about the placement of a window on the screen.
	/// </summary>
	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct WindowPlacement
	{
		/// <summary>
		/// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
		/// <para>
		/// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
		/// </para>
		/// </summary>
		public int Length;

		/// <summary>
		/// Specifies flags that control the position of the minimized window and the method by which the window is restored.
		/// </summary>
		public WindowPlacementFlags Flags;

		/// <summary>
		/// The current show state of the window.
		/// </summary>
		public ShowWindowCommand ShowCmd;

		/// <summary>
		/// The coordinates of the window's upper-left corner when the window is minimized.
		/// </summary>
		public POINT MinPosition;

		/// <summary>
		/// The coordinates of the window's upper-left corner when the window is maximized.
		/// </summary>
		public POINT MaxPosition;

		/// <summary>
		/// The window's coordinates when the window is in the restored position.
		/// </summary>
		public RECT NormalPosition;

		/// <summary>
		/// Gets the default (empty) value.
		/// </summary>
		public static WindowPlacement Default
		{
			get
			{
				WindowPlacement result = new WindowPlacement();
				result.Length = Marshal.SizeOf(result);
				return result;
			}
		}
	}
}
