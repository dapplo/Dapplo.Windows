// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.SafeHandles;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms648052(v=vs.85).aspx">ICONINFO structure</a>
/// Contains information about an icon or a cursor.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IconInfo
{
	private bool _fIcon;
	private int _xHotspot;
	private int _yHotspot;
	private readonly IntPtr _hbmMask;
	private readonly IntPtr _hbmColor;

	/// <summary>
	/// Specifies whether this structure defines an icon or a cursor.
	/// A value of TRUE specifies an icon; FALSE specifies a cursor.
	/// </summary>
	public bool IsIcon
	{
		get
		{
			return _fIcon;
		}
		set
		{
			_fIcon = value;
		}
	}

	/// <summary>
	/// The x and y coordinates of a cursor's hot spot.
	/// If this structure defines an icon, the hot spot is always in the center of the icon,
	/// and this member is ignored.
	/// </summary>
	public NativePoint Hotspot
	{
		get
		{
			return new NativePoint(_xHotspot, _yHotspot);
		}
		set
		{
			_xHotspot = value.X;
			_yHotspot = value.Y;
		}
	}


	/// <summary>
	/// The icon bitmask bitmap.
	/// If this structure defines a black and white icon, this bitmask is formatted so that the upper half is the icon AND bitmask and the lower half is the icon XOR bitmask.
	/// Under this condition, the height should be an even multiple of two.
	/// If this structure defines a color icon, this mask only defines the AND bitmask of the icon.
	/// </summary>
	public SafeHBitmapHandle BitmaskBitmapHandle => new SafeHBitmapHandle(_hbmMask);

	/// <summary>
	/// A handle to the icon color bitmap.
	/// This member can be optional if this structure defines a black and white icon.
	/// The AND bitmask of hbmMask is applied with the SRCAND flag to the destination;
	/// subsequently, the color bitmap is applied (using XOR) to the destination by using the SRCINVERT flag.
	/// </summary>
	public SafeHBitmapHandle ColorBitmapHandle => new SafeHBitmapHandle(_hbmColor);
}