// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com.Structs;

/// <summary>
/// Defines the filter specifications used in common file dialogs, containing a description and pattern string.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shtypes/ns-shtypes-comdlg_filterspec">COMDLG_FILTERSPEC structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
public struct FilterSpec
{
    /// <summary>
    /// A pointer to a buffer that contains the friendly name for the filter, such as "Text Files (.txt)".
    /// </summary>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Name;

    /// <summary>
    /// A pointer to a buffer that contains the file pattern, such as "*.txt".
    /// </summary>
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Spec;

    /// <summary>
    /// Initializes a new instance of <see cref="FilterSpec"/> with the given name and file extension pattern.
    /// </summary>
    /// <param name="name">Friendly name for the filter, e.g. "Text Files (.txt)"</param>
    /// <param name="spec">File pattern, e.g. "*.txt"</param>
    public FilterSpec(string name, string spec)
    {
        Name = name;
        Spec = spec;
    }
}
