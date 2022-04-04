// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums;

/// <summary>
///     An enum specifying the type of input event used for the SendInput call.
///     This specifies which structure type of the union supplied to SendInput is used.
///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646270(v=vs.85).aspx">INPUT structure</a>
/// </summary>
public enum InputTypes : uint
{
    /// <summary>
    ///     The event is a mouse event.
    /// </summary>
    Mouse = 0,

    /// <summary>
    ///     The event is a keyboard event.
    /// </summary>
    Keyboard = 1,

    /// <summary>
    ///     The event is a hardware event.
    /// </summary>
    Hardware = 2
}