// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Shell32.Structs;

/// <summary>
/// A structure which describes shell32 info on a file
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct ShellFileInfo
{
    private readonly IntPtr _hIcon;
    private readonly int _iIcon;
    private readonly uint _dwAttributes;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    private readonly string _szDisplayName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    private readonly string _szTypeName;

    /// <summary>
    /// A handle to the icon that represents the file.
    /// You are responsible for destroying this handle with DestroyIcon when you no longer need it.
    /// </summary>
    public IntPtr IconHandle => _hIcon;

    /// <summary>
    /// The index of the icon image within the system image list.
    /// </summary>
    public int IconIndex => _iIcon;

    /// <summary>
    /// An array of values that indicates the attributes of the file object.
    /// For information about these values, see the IShellFolder::GetAttributesOf method.
    /// </summary>
    public uint Attributes => _dwAttributes;

    /// <summary>
    /// A string that contains the name of the file as it appears in the Windows Shell, or the path and file name of the file that contains the icon representing the file.
    /// </summary>
    public string DisplayName => _szDisplayName;

    /// <summary>
    /// A string that describes the type of file
    /// </summary>
    public string TypeName => _szTypeName;
}