// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.User32.Enums;

/// <summary>
///     Get/Set WindowLong Enum See: http://msdn.microsoft.com/en-us/library/ms633591.aspx
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum WindowLongIndex
{
    /// <summary>
    ///     Sets a new extended window style.
    /// </summary>
    GWL_EXSTYLE = -20,

    /// <summary>
    ///     Sets a new application instance handle.
    /// </summary>
    GWL_HINSTANCE = -6,

    /// <summary>
    ///     Sets a new identifier of the child window. The window cannot be a top-level window.
    /// </summary>
    GWL_ID = -12,

    /// <summary>
    ///     Sets a new window style.
    /// </summary>
    GWL_STYLE = -16,

    /// <summary>
    ///     Sets the user data associated with the window.
    ///     This data is intended for use by the application that created the window.
    ///     Its value is initially zero.
    /// </summary>
    GWL_USERDATA = -21,

    /// <summary>
    ///     Sets a new address for the window procedure.
    ///     You cannot change this attribute if the window does not belong to the same process as the calling thread.
    /// </summary>
    GWL_WNDPROC = -4
}