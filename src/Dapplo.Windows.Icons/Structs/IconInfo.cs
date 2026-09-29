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
/// <para>
/// When filled by GetIconInfo, the two bitmaps (mask and color) are created for the caller, who must delete them exactly once.
/// The properties <see cref="BitmaskBitmap"/> and <see cref="ColorBitmap"/> only expose the raw, non-owning, handles.
/// Use <see cref="TakeBitmaps"/> to transfer the ownership into SafeHandles, or <see cref="DeleteBitmaps"/> to delete them.
/// As this is a struct, copies share the same handles: only take or delete the bitmaps on one copy.
/// When used for CreateIconIndirect, the bitmaps stay owned by the caller (the system copies them).
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct IconInfo
{
	private bool _fIcon;
	private int _xHotspot;
	private int _yHotspot;
	private IntPtr _hbmMask;
	private IntPtr _hbmColor;

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
	/// <remarks>This is the raw handle, it is not owned by this struct nor by the caller of the getter, see <see cref="TakeBitmaps"/>.</remarks>
	public IntPtr BitmaskBitmap
	{
		get => _hbmMask;
		set => _hbmMask = value;
	}

	/// <summary>
	/// A handle to the icon color bitmap.
	/// This member can be optional if this structure defines a black and white icon.
	/// The AND bitmask of hbmMask is applied with the SRCAND flag to the destination;
	/// subsequently, the color bitmap is applied (using XOR) to the destination by using the SRCINVERT flag.
	/// </summary>
	/// <remarks>This is the raw handle, it is not owned by this struct nor by the caller of the getter, see <see cref="TakeBitmaps"/>. IntPtr.Zero for a monochrome icon or cursor.</remarks>
	public IntPtr ColorBitmap
	{
		get => _hbmColor;
		set => _hbmColor = value;
	}

	/// <summary>
	/// Transfer the ownership of the bitmaps, which were created by GetIconInfo, to the caller.
	/// The handles in this struct are cleared, so calling this method (or <see cref="DeleteBitmaps"/>) again on the same variable does nothing.
	/// </summary>
	/// <param name="bitmaskBitmap">SafeHBitmapHandle owning the bitmask bitmap, dispose it when done</param>
	/// <param name="colorBitmap">SafeHBitmapHandle owning the color bitmap, dispose it when done. This is invalid for a monochrome icon or cursor.</param>
	public void TakeBitmaps(out SafeHBitmapHandle bitmaskBitmap, out SafeHBitmapHandle colorBitmap)
	{
		bitmaskBitmap = new SafeHBitmapHandle(_hbmMask);
		colorBitmap = new SafeHBitmapHandle(_hbmColor);
		_hbmMask = IntPtr.Zero;
		_hbmColor = IntPtr.Zero;
	}

	/// <summary>
	/// Deletes the bitmaps which were created by GetIconInfo, and clears the handles in this struct.
	/// Only call this on one copy of the struct, and not when the ownership was already taken with <see cref="TakeBitmaps"/>.
	/// </summary>
	public void DeleteBitmaps()
	{
		TakeBitmaps(out var bitmaskBitmap, out var colorBitmap);
		bitmaskBitmap.Dispose();
		colorBitmap.Dispose();
	}
}