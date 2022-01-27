// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace Dapplo.Windows.Input.Enums;

/// <summary>
/// Defines which mouse buttons to use
/// </summary>
[Flags]
public enum MouseButtons
{
    /// <summary>
    /// No button
    /// </summary>
    None,
    /// <summary>
    /// Left mouse button
    /// </summary>
    Left = 0x100000,
    /// <summary>
    /// Right mouse button
    /// </summary>
    Right = 0x200000,
    /// <summary>
    /// Middle mouse button
    /// </summary>
    Middle = 0x400000,
    /// <summary>
    /// Extra button 1
    /// </summary>
    XButton1 = 0x800000,
    /// <summary>
    /// Extra button 2
    /// </summary>
    XButton2 = 0x1000000,
}