// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Dapplo.Windows.Gdi32.SafeHandles;
using Dapplo.Windows.User32;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace Dapplo.Windows.Icons.Structs;

/// <summary>
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-iconinfoexw">ICONINFOEX structure</a>
/// Contains information about an icon or a cursor.
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public unsafe struct IconInfoEx
{
	private int _cbSize;
	private bool _fIcon;
	private int _xHotspot;
	private int _yHotspot;
	private readonly IntPtr _hbmMask;
	private readonly IntPtr _hbmColor;
	private readonly ushort _wResID;

    private fixed char _szModName[260];
    private fixed char _szResName[260];

    /// <summary>
    /// Specifies whether this structure defines an icon or a cursor.
    /// A value of TRUE specifies an icon; FALSE specifies a cursor.
    /// </summary>
    public bool IsIcon
    {
        get => _fIcon;
		set => _fIcon = value;
    }

    /// <summary>
    /// The x and y coordinates of a cursor's hot spot.
    /// If this structure defines an icon, the hot spot is always in the center of the icon,
    /// and this member is ignored.
    /// </summary>
    public NativePoint Hotspot
    {
        get => new NativePoint(_xHotspot, _yHotspot);
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

    /// <summary>
    /// Resource identifier of the resource in szModName module. If the icon or cursor was loaded by name, then wResID is zero and szResName contains the resource name.
    /// </summary>
    public ushort ResourceId => _wResID;

    /// <summary>
    ///     Name of the module from which an icon or a cursor was loaded.
    ///     You can use GetModuleHandle function to convert it to the module handle compatible with the resource-management functions.
    /// </summary>
    public string ModuleName
    {
        get
        {
            fixed (char* moduleName = _szModName)
            {
                return new string(moduleName);
            }

        }
    }

    /// <summary>
    ///     Resource name of the resource in ModuleName module.
    /// </summary>
    public string ResourceName
    {
        get
        {
            fixed (char* resourceName = _szResName)
            {
                return new string(resourceName);
            }

        }
    }

    /// <summary>
    ///     Create a IconInfoEx with defaults
    /// </summary>
    public static IconInfoEx Create()
    {
        return new IconInfoEx
        {
            _cbSize = Marshal.SizeOf(typeof(IconInfoEx))
        };
    }
}