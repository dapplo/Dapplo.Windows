//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32.Enums;

#endregion

namespace Dapplo.Windows.User32.Structs
{
	/// <summary>
	///     The structure for the WINDOWINFO
	///     See <a href="http://msdn.microsoft.com/en-us/library/windows/desktop/ms632610.aspx">WINDOWINFO struct</a>
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential)]
	public struct WindowInfo
	{
		/// <summary>
		/// The size of the structure, in bytes. The caller must set this member to sizeof(WINDOWINFO).
		/// </summary>
		private uint _cbSize;
		private NativeRect _rcWindow;
		private NativeRect _rcClient;
		private readonly WindowStyleFlags _dwStyle;
		private readonly ExtendedWindowStyleFlags _dwExStyle;
		/// <summary>
		/// The window status. If this member is WS_ACTIVECAPTION (0x0001), the window is active. Otherwise, this member is zero.
		/// </summary>
		private readonly uint _dwWindowStatus;

		/// <summary>
		/// The width of the window border, in pixels.
		/// </summary>
		private readonly uint _cxWindowBorders;
		/// <summary>
		/// The height of the window border, in pixels.
		/// </summary>
		private readonly uint _cyWindowBorders;

		private readonly ushort _atomWindowType;
		private readonly ushort _wCreatorVersion;

		/// <summary>
		/// Test if the window is active
		/// </summary>
		public bool IsActive => _dwWindowStatus == 1;

		/// <summary>
		/// The coordinates of the window, or client if the Window is returned as empty.
		/// </summary>
		public NativeRect Bounds
		{
			get => _rcWindow;
			set => _rcWindow = value;
		}

		/// <summary>
		/// The coordinates of the client area.
		/// </summary>
		public NativeRect ClientBounds
		{
			get => _rcClient;
			set => _rcClient = value;
		}

		/// <summary>
		/// The window styles.
		/// </summary>
		public WindowStyleFlags Style => _dwStyle;

		/// <summary>
		/// The extended window styles. 
		/// </summary>
		public ExtendedWindowStyleFlags ExtendedStyle => _dwExStyle;

		/// <summary>
		/// The size of the border
		/// </summary>
		public NativeSize BorderSize => new NativeSize((int) _cxWindowBorders,(int) _cyWindowBorders);

		/// <summary>
		/// The Windows version of the application that created the window.
		/// </summary>
		public ushort CreatorVersion => _wCreatorVersion;

		/// <summary>
		/// The window class atom.
		/// </summary>
		public ushort AtomWindowType => _atomWindowType;

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{{IsActive: {IsActive}; Bounds: {Bounds}; ClientBounds: {ClientBounds}; Style: {Style}; ExtendedStyle: {ExtendedStyle}; BorderSize: {BorderSize};}}";
		}

		/// <summary>
		///     Factory method for a default WindowInfo.
		/// </summary>
		public static WindowInfo Create()
		{
			return new WindowInfo
			{
				_cbSize = (uint) Marshal.SizeOf(typeof(WindowInfo))
			};
		}
	}
}