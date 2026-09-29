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
    /// Test if the VirtualKeyCode is a modifier key: Shift, Control, Alt (Menu) or Windows, left, right or generic.
    /// The lock/toggle keys (CapsLock, NumLock, ScrollLock) are not modifiers, see <see cref="IsToggleKey"/>.
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
            case VirtualKeyCode.Win:
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

    /// <summary>
    /// Test if the VirtualKeyCode is a generic key, which stands for its left and right variant: Shift, Control, Menu (Alt) or the pseudo code Win.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool</returns>
    public static bool IsGeneric(this VirtualKeyCode virtualKeyCode)
    {
        return virtualKeyCode is VirtualKeyCode.Shift or VirtualKeyCode.Control or VirtualKeyCode.Menu or VirtualKeyCode.Win;
    }

    /// <summary>
    /// Test if the specified key matches the expected key, a generic expected key (Shift, Control, Menu or Win) matches its left and right variant.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode, the key which was pressed</param>
    /// <param name="expected">VirtualKeyCode, the key which is expected</param>
    /// <returns>bool true if the key matches</returns>
    public static bool Matches(this VirtualKeyCode virtualKeyCode, VirtualKeyCode expected)
    {
        if (virtualKeyCode == expected)
        {
            return true;
        }

        return expected switch
        {
            VirtualKeyCode.Shift => virtualKeyCode is VirtualKeyCode.LeftShift or VirtualKeyCode.RightShift,
            VirtualKeyCode.Control => virtualKeyCode is VirtualKeyCode.LeftControl or VirtualKeyCode.RightControl,
            VirtualKeyCode.Menu => virtualKeyCode is VirtualKeyCode.LeftMenu or VirtualKeyCode.RightMenu,
            VirtualKeyCode.Win => virtualKeyCode is VirtualKeyCode.LeftWin or VirtualKeyCode.RightWin,
            _ => false
        };
    }
}