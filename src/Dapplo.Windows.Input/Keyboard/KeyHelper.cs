﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
///     Some code to help with hotkeys
/// </summary>
public static class KeyHelper
{
    private const int DoNotCareLeftRight = 0b_00000010_00000000_00000000_00000000;
    private const int Extended = 0b_00000001_00000000_00000000_00000000;

    /// <summary>
    ///     Get the name of a key, in the keyboard locale
    /// </summary>
    /// <param name="givenKey">VirtualKeyCode</param>
    /// <param name="doNotCare">bool, default true</param>
    /// <returns>string</returns>
    public static string VirtualCodeToLocaleDisplayText(VirtualKeyCode givenKey, bool doNotCare = true)
    {
        unsafe
        {
            const int capacity = 100;
            var keyName = stackalloc char[capacity];
            const uint numpad = 55;
            uint scancodeModifier = 0;

            var virtualKey = givenKey;
            string keyString;
            int nrCharacters;
            if (doNotCare)
            {
                scancodeModifier |= DoNotCareLeftRight; // set "Do not care" bit

                switch (virtualKey)
                {
                    case VirtualKeyCode.LeftMenu:
                    case VirtualKeyCode.RightMenu:
                        virtualKey = VirtualKeyCode.Menu;
                        break;
                    case VirtualKeyCode.LeftControl:
                    case VirtualKeyCode.RightControl:
                        virtualKey = VirtualKeyCode.Control;
                        break;
                    case VirtualKeyCode.LeftShift:
                    case VirtualKeyCode.RightShift:
                        virtualKey = VirtualKeyCode.Shift;
                        break;
                }
            }
            // Map virtual key codes to real keys
            switch (virtualKey)
            {
                case VirtualKeyCode.Multiply:
                    nrCharacters = GetKeyNameText(numpad << 16, keyName, 100);

                    keyString = new string(keyName,0, nrCharacters).Replace("*", "").Trim().ToLower();
                    if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                    {
                        return "* " + keyString;
                    }
                    keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                    return keyString + " *";
                case VirtualKeyCode.Divide:
                    nrCharacters = GetKeyNameText(numpad << 16, keyName, capacity);
                    keyString = new string(keyName, 0, nrCharacters).Replace("*", "").Trim().ToLower();
                    if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                    {
                        return "/ " + keyString;
                    }
                    keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                    return keyString + " /";
            }

            var keyboardLayout = GetKeyboardLayout(0);
            var scanCode = MapVirtualKeyEx((uint) virtualKey, (uint) MapVkType.VkToVscEx, keyboardLayout);

            // No value found
            if (scanCode == 0)
            {
                return givenKey.ToString();
            }

            // because MapVirtualKey strips the extended bit for some keys
            switch (virtualKey)
            {
                case VirtualKeyCode.Left:
                case VirtualKeyCode.Up:
                case VirtualKeyCode.Right:
                case VirtualKeyCode.Down: // arrow keys
                case VirtualKeyCode.Prior:
                case VirtualKeyCode.Next: // page up and page down
                case VirtualKeyCode.End:
                case VirtualKeyCode.Home:
                case VirtualKeyCode.Insert:
                case VirtualKeyCode.Delete:
                case VirtualKeyCode.NumLock:
                    scancodeModifier |= Extended; // set extended bit
                    break;
                case VirtualKeyCode.Print: // PrintScreen
                    scanCode = 311;
                    break;
                case VirtualKeyCode.Pause: // Pause
                    scanCode = 69;
                    break;
            }

            scanCode <<= 16;
            scanCode |= scancodeModifier;
            nrCharacters = GetKeyNameText(scanCode, keyName, capacity);
            if (nrCharacters == 0)
            {
                return givenKey.ToString();
            }

            var visibleName = new string(keyName, 0, nrCharacters);
            if (visibleName.Length > 1)
            {
                visibleName = visibleName.Substring(0, 1) + visibleName.Substring(1).ToLower();
            }
            return visibleName;
        }
    }

    /// <summary>
    ///     Get the VirtualKeyCodes from a key combination description
    /// </summary>
    /// <param name="keyDescription">string with the key combination</param>
    /// <returns>IEnumerable with VirtualKeyCodes</returns>
    public static IEnumerable<VirtualKeyCode> VirtualKeyCodesFromString(string keyDescription)
    {
        if (string.IsNullOrEmpty(keyDescription))
        {
            return Enumerable.Empty<VirtualKeyCode>();
        }

        return keyDescription
            .Split('+')
            .Select(part => part.Trim())
            .Where(part => !string.IsNullOrEmpty(part))
            .Select(VirtualKeyCodeFromString)
            .Where(vk => vk != VirtualKeyCode.None);
    }


    /// <summary>
    ///     Get a VirtualKeyCodes from a string
    /// </summary>
    /// <param name="keyDescription">string with the key</param>
    /// <returns>VirtualKeyCodes</returns>
    public static VirtualKeyCode VirtualKeyCodeFromString(string keyDescription)
    {
        if (string.IsNullOrEmpty(keyDescription))
        {
            return VirtualKeyCode.None;
        }

        if (keyDescription.Length == 1)
        {
            keyDescription = "KEY" + keyDescription;
        }
        if (Enum.TryParse(keyDescription, true, out VirtualKeyCode result))
        {
            return result;
        }

        keyDescription = keyDescription.ToLowerInvariant();
            
        // Border cases
        return keyDescription switch
        {
            "alt" => VirtualKeyCode.Menu,
            "ctrl" => VirtualKeyCode.Control,
            "win" => VirtualKeyCode.LeftWin,
            "shift" => VirtualKeyCode.Shift,
            _ => VirtualKeyCode.None
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lParam"></param>
    /// <param name="lpString"></param>
    /// <param name="nSize"></param>
    /// <returns>int with the number of characters returned</returns>
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern unsafe int GetKeyNameText(uint lParam, [Out] char* lpString, int nSize);
}