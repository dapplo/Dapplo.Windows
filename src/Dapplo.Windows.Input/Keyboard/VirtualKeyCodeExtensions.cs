// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
/// Extensions for VirtualKeyCode
/// </summary>
public static class VirtualKeyCodeExtensions
{
    /// <summary>
    /// Test if the VirtualKeyCode is a modifier key
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool</returns>
    public static bool IsModifier(this VirtualKeyCode virtualKeyCode)
    {
        bool isModifier = false;
        switch (virtualKeyCode)
        {
            case VirtualKeyCode.LeftShift:
            case VirtualKeyCode.Shift:
            case VirtualKeyCode.RightShift:
            case VirtualKeyCode.Control:
            case VirtualKeyCode.LeftControl:
            case VirtualKeyCode.RightControl:
            case VirtualKeyCode.Menu:
            case VirtualKeyCode.LeftMenu:
            case VirtualKeyCode.RightMenu:
            case VirtualKeyCode.LeftWin:
            case VirtualKeyCode.RightWin:
                isModifier = true;
                break;
        }

        return isModifier;
    }

    /// <summary>
    /// Test if the VirtualKeyCode is a toggle/lock key (CapsLock, NumLock, ScrollLock)
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool</returns>
    public static bool IsToggleKey(this VirtualKeyCode virtualKeyCode)
    {
        return virtualKeyCode is VirtualKeyCode.Capital or VirtualKeyCode.NumLock or VirtualKeyCode.Scroll;
    }
}