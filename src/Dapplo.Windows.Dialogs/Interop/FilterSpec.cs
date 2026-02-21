// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Dapplo.Windows.Dialogs.Interop;

/// <summary>
/// Defines the filter specifications used in common file dialogs.
/// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/shtypes/ns-shtypes-comdlg_filterspec">COMDLG_FILTERSPEC structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct FilterSpec
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public string Name;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string Spec;

    public FilterSpec(string name, string spec)
    {
        Name = name;
        Spec = spec;
    }
}
