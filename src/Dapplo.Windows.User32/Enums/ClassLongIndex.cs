﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633580(v=vs.85).aspx">GetClassLong function</a>
/// </summary>
public enum ClassLongIndex
{
    /// <summary>
    ///     Retrieves an ATOM value that uniquely identifies the window class.
    ///     This is the same atom that the RegisterClassEx function returns.
    /// </summary>
    Atom = -32,

    /// <summary>
    ///     the size, in bytes, of the extra memory associated with the class.
    ///     Setting this value does not change the number of extra bytes already allocated.
    /// </summary>
    ClassExtraBytes = -20,

    /// <summary>
    ///     the size, in bytes, of the extra window memory associated with each window in the class.
    ///     Setting this value does not change the number of extra bytes already allocated.
    ///     For information on how to access this memory, see SetWindowLong.
    /// </summary>
    WindowExtraBytes = -18,

    /// <summary>
    ///     a handle to the background brush associated with the class.
    /// </summary>
    BackgroundBrushHandle = -10,

    /// <summary>
    ///     a handle to the cursor associated with the class.
    /// </summary>
    CursorHandle = -12,

    /// <summary>
    ///     GCL_HICON a handle to the icon associated with the class.
    /// </summary>
    IconHandle = -14,

    /// <summary>
    ///     GCL_HICONSM a handle to the small icon associated with the class.
    /// </summary>
    SmallIconHandle = -34,

    /// <summary>
    ///     a handle to the module that registered the class.
    /// </summary>
    ModuleHandle = -16,

    /// <summary>
    ///     the address of the menu name string. The string identifies the menu resource associated with the class.
    /// </summary>
    MenuName = -8,

    /// <summary>
    ///     the window-class style bits.
    /// </summary>
    Style = -26,

    /// <summary>
    ///     the address of the window procedure, or a handle representing the address of the window procedure.
    ///     You must use the CallWindowProc function to call the window procedure.
    /// </summary>
    WindowProc = -24
}