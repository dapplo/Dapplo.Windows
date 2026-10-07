// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Automation.Interop;

/// <summary>
///     The few user32 functions this package needs
/// </summary>
internal static class NativeMethods
{
    private const string User32 = "user32.dll";

    [DllImport(User32)]
    internal static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport(User32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool IsChild(IntPtr parentWindowHandle, IntPtr windowHandle);
}
