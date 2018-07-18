//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Dapplo.Windows.Input.Enums;

#endregion

namespace Dapplo.Windows.Input.Keyboard
{
    /// <summary>
    ///     Some code to help with hotkeys
    /// </summary>
    public static class KeyHelper
    {
        /// <summary>
        ///     Get the name of a key
        /// </summary>
        /// <param name="givenKey">Keys</param>
        /// <returns>string</returns>
        public static string GetKeyName(Keys givenKey)
        {
            unsafe
            {
                const int capacity = 100;
                var keyName = stackalloc char[capacity];
                const uint numpad = 55;

                var virtualKey = givenKey;
                string keyString;
                int nrCharacters;
                // Make VC's to real keys
                switch (virtualKey)
                {
                    case Keys.Alt:
                        virtualKey = Keys.LMenu;
                        break;
                    case Keys.Control:
                        virtualKey = Keys.ControlKey;
                        break;
                    case Keys.Shift:
                        virtualKey = Keys.LShiftKey;
                        break;
                    case Keys.Multiply:
                        nrCharacters = GetKeyNameText(numpad << 16, keyName, 100);

                        keyString = new string(keyName,0, nrCharacters).Replace("*", "").Trim().ToLower();
                        if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                        {
                            return "* " + keyString;
                        }
                        keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                        return keyString + " *";
                    case Keys.Divide:
                        nrCharacters = GetKeyNameText(numpad << 16, keyName, capacity);
                        keyString = new string(keyName, 0, nrCharacters).Replace("*", "").Trim().ToLower();
                        if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
                        {
                            return "/ " + keyString;
                        }
                        keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
                        return keyString + " /";
                }
                var scanCode = MapVirtualKey((uint)virtualKey, (uint)MapVkType.VkToVsc);

                // because MapVirtualKey strips the extended bit for some keys
                switch (virtualKey)
                {
                    case Keys.Left:
                    case Keys.Up:
                    case Keys.Right:
                    case Keys.Down: // arrow keys
                    case Keys.Prior:
                    case Keys.Next: // page up and page down
                    case Keys.End:
                    case Keys.Home:
                    case Keys.Insert:
                    case Keys.Delete:
                    case Keys.NumLock:
                        scanCode |= 0x100; // set extended bit
                        break;
                    case Keys.PrintScreen: // PrintScreen
                        scanCode = 311;
                        break;
                    case Keys.Pause: // PrintScreen
                        scanCode = 69;
                        break;
                }
                scanCode |= 0x200;
                nrCharacters = GetKeyNameText(scanCode << 16, keyName, capacity);
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
        ///     Create a localized string from a normal hotkey string
        /// </summary>
        /// <param name="hotkeyString">string</param>
        /// <returns>string</returns>
        public static string GetLocalizedHotkeyStringFromString(string hotkeyString)
        {
            var virtualKeyCode = KeyFromString(hotkeyString);
            var modifiers = KeyModifiersFromString(hotkeyString);
            return KeyToLocalizedString(modifiers, virtualKeyCode);
        }

        /// <summary>
        ///     Get a Key object from a hotkey description
        /// </summary>
        /// <param name="hotkey">string with the hotkey</param>
        /// <returns>Keys</returns>
        public static Keys KeyFromString(string hotkey)
        {
            if (string.IsNullOrEmpty(hotkey))
            {
                return Keys.None;
            }
            if (hotkey.LastIndexOf('+') > 0)
            {
                hotkey = hotkey.Remove(0, hotkey.LastIndexOf('+') + 1).Trim();
            }
            return (Keys) Enum.Parse(typeof(Keys), hotkey, false);
        }


        /// <summary>
        ///     Get the modifiers as a Keys enum
        /// </summary>
        /// <param name="modifiersString"></param>
        /// <returns>Keys</returns>
        public static Keys KeyModifiersFromString(string modifiersString)
        {
            if (string.IsNullOrEmpty(modifiersString))
            {
                return Keys.None;
            }

            var modifiers = Keys.None;
            if (modifiersString.ToLower().Contains("alt"))
            {
                modifiers |= Keys.Alt;
            }
            if (modifiersString.ToLower().Contains("ctrl"))
            {
                modifiers |= Keys.Control;
            }
            if (modifiersString.ToLower().Contains("shift"))
            {
                modifiers |= Keys.Shift;
            }
            if (modifiersString.ToLower().Contains("win"))
            {
                modifiers |= Keys.LWin;
            }
            return modifiers;
        }

        /// <summary>
        ///     Create a localized string from the specified modifiers keys
        /// </summary>
        /// <param name="modifierKeyCode">Keys</param>
        /// <returns>string</returns>
        public static string KeyModifiersToLocalizedString(Keys modifierKeyCode)
        {
            var hotkeyString = new StringBuilder();
            if ((modifierKeyCode & Keys.Alt) > 0)
            {
                hotkeyString.Append(GetKeyName(Keys.Alt)).Append(" + ");
            }
            if ((modifierKeyCode & Keys.Control) > 0)
            {
                hotkeyString.Append(GetKeyName(Keys.Control)).Append(" + ");
            }
            if ((modifierKeyCode & Keys.Shift) > 0)
            {
                hotkeyString.Append(GetKeyName(Keys.Shift)).Append(" + ");
            }
            if (modifierKeyCode == Keys.LWin || modifierKeyCode == Keys.RWin)
            {
                hotkeyString.Append("Win").Append(" + ");
            }
            return hotkeyString.ToString();
        }

        /// <summary>
        ///     Create a string of the specified modifiers
        /// </summary>
        /// <param name="modifierKeyCode">Keys</param>
        /// <returns>string</returns>
        public static string KeyModifiersToString(Keys modifierKeyCode)
        {
            var hotkeyString = new StringBuilder();
            if ((modifierKeyCode & Keys.Alt) > 0)
            {
                hotkeyString.Append("Alt").Append(" + ");
            }
            if ((modifierKeyCode & Keys.Control) > 0)
            {
                hotkeyString.Append("Ctrl").Append(" + ");
            }
            if ((modifierKeyCode & Keys.Shift) > 0)
            {
                hotkeyString.Append("Shift").Append(" + ");
            }
            if (modifierKeyCode == Keys.LWin || modifierKeyCode == Keys.RWin)
            {
                hotkeyString.Append("Win").Append(" + ");
            }
            return hotkeyString.ToString();
        }


        /// <summary>
        ///     Create a localized string from the specified keys
        /// </summary>
        /// <param name="modifierKeyCode">Keys</param>
        /// <param name="virtualKeyCode">Keys</param>
        /// <returns>string</returns>
        public static string KeyToLocalizedString(Keys modifierKeyCode, Keys virtualKeyCode)
        {
            return KeyModifiersToLocalizedString(modifierKeyCode) + GetKeyName(virtualKeyCode);
        }

        /// <summary>
        ///     Create a string for the specified modifiers and key
        /// </summary>
        /// <param name="modifierKeyCode">Keys</param>
        /// <param name="virtualKeyCode">Keys</param>
        /// <returns>string</returns>
        public static string KeyToString(Keys modifierKeyCode, Keys virtualKeyCode)
        {
            return KeyModifiersToString(modifierKeyCode) + virtualKeyCode;
        }

        #region Native

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="lParam"></param>
        /// <param name="lpString"></param>
        /// <param name="nSize"></param>
        /// <returns>int with the number of characters returned</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern unsafe int GetKeyNameText(uint lParam, [Out] char* lpString, int nSize);

        #endregion
    }
}