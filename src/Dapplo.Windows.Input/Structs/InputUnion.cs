// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     A "union" containing a specific input struct
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct InputUnion
{
    /// <summary>
    ///     Assign this if the input is MouseInput
    /// </summary>
    [FieldOffset(0)] public MouseInput MouseInput;

    /// <summary>
    ///     Assign this if the input is MouseInputKeyboardInput
    /// </summary>
    [FieldOffset(0)] public KeyboardInput KeyboardInput;

    /// <summary>
    ///     Assign this if the input is HardwareInput
    /// </summary>
    [FieldOffset(0)] public HardwareInput HardwareInput;
}