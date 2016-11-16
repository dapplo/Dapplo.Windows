//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016 Dapplo
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
using Dapplo.Log;

#endregion

namespace Dapplo.Windows
{
	internal enum MapVkType : uint
	{
		/// <summary>
		///     The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does
		///     not distinguish between left- and right-hand keys, the left-hand scan code is returned. If there is no translation,
		///     the function returns 0.
		/// </summary>
		VkToVsc = 0,

		/// <summary>
		///     The uCode parameter is a scan code and is translated into a virtual-key code that does not distinguish between
		///     left- and right-hand keys. If there is no translation, the function returns 0.
		/// </summary>
		VscToVk = 1,

		/// <summary>
		///     The uCode parameter is a virtual-key code and is translated into an unshifted character value in the low order word
		///     of the return value. Dead keys (diacritics) are indicated by setting the top bit of the return value. If there is
		///     no translation, the function returns 0.
		/// </summary>
		VkToChar = 2,

		/// <summary>
		///     The uCode parameter is a scan code and is translated into a virtual-key code that distinguishes between left- and
		///     right-hand keys. If there is no translation, the function returns 0.
		/// </summary>
		VscToVkEx = 3,

		/// <summary>
		///     The uCode parameter is a virtual-key code and is translated into a scan code. If it is a virtual-key code that does
		///     not distinguish between left- and right-hand keys, the left-hand scan code is returned. If the scan code is an
		///     extended scan code, the high byte of the uCode value can contain either 0xe0 or 0xe1 to specify the extended scan
		///     code. If there is no translation, the function returns 0.
		/// </summary>
		VkToVscEx = 4
	}

	/// <summary>
	/// Some code to help with hotkeys
	/// </summary>
	public class Hotkey
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     Get the name of a key
		/// </summary>
		/// <param name="givenKey"></param>
		/// <returns></returns>
		public static string GetKeyName(Keys givenKey)
		{
			var keyName = new StringBuilder();
			const uint numpad = 55;

			var virtualKey = givenKey;
			string keyString;
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
					GetKeyNameText(numpad << 16, keyName, 100);
					keyString = keyName.ToString().Replace("*", "").Trim().ToLower();
					if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
					{
						return "* " + keyString;
					}
					keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
					return keyString + " *";
				case Keys.Divide:
					GetKeyNameText(numpad << 16, keyName, 100);
					keyString = keyName.ToString().Replace("*", "").Trim().ToLower();
					if (keyString.IndexOf("(", StringComparison.Ordinal) >= 0)
					{
						return "/ " + keyString;
					}
					keyString = keyString.Substring(0, 1).ToUpper() + keyString.Substring(1).ToLower();
					return keyString + " /";
			}
			var scanCode = MapVirtualKey((uint) virtualKey, (uint) MapVkType.VkToVsc);

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
					Log.Debug().WriteLine("Modifying Extended bit");
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
			if (GetKeyNameText(scanCode << 16, keyName, 100) != 0)
			{
				var visibleName = keyName.ToString();
				if (visibleName.Length > 1)
				{
					visibleName = visibleName.Substring(0, 1) + visibleName.Substring(1).ToLower();
				}
				return visibleName;
			}
			return givenKey.ToString();
		}

		/// <summary>
		///     Create a localized string from a normal hotkey string
		/// </summary>
		/// <param name="hotkeyString">string</param>
		/// <returns>string</returns>
		public static string GetLocalizedHotkeyStringFromString(string hotkeyString)
		{
			var virtualKeyCode = HotkeyFromString(hotkeyString);
			var modifiers = HotkeyModifiersFromString(hotkeyString);
			return HotkeyToLocalizedString(modifiers, virtualKeyCode);
		}

		/// <summary>
		///     Get a Key object from a hotkey description
		/// </summary>
		/// <param name="hotkey">string with the hotkey</param>
		/// <returns>Keys</returns>
		public static Keys HotkeyFromString(string hotkey)
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
		public static Keys HotkeyModifiersFromString(string modifiersString)
		{
			var modifiers = Keys.None;
			if (!string.IsNullOrEmpty(modifiersString))
			{
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
			}
			return modifiers;
		}

		/// <summary>
		///     Create a localized string from the specified modifiers keys
		/// </summary>
		/// <param name="modifierKeyCode">Keys</param>
		/// <returns>string</returns>
		public static string HotkeyModifiersToLocalizedString(Keys modifierKeyCode)
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
			if ((modifierKeyCode == Keys.LWin) || (modifierKeyCode == Keys.RWin))
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
		public static string HotkeyModifiersToString(Keys modifierKeyCode)
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
			if ((modifierKeyCode == Keys.LWin) || (modifierKeyCode == Keys.RWin))
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
		public static string HotkeyToLocalizedString(Keys modifierKeyCode, Keys virtualKeyCode)
		{
			return HotkeyModifiersToLocalizedString(modifierKeyCode) + GetKeyName(virtualKeyCode);
		}

		/// <summary>
		///     Create a string for the specified modifiers and key
		/// </summary>
		/// <param name="modifierKeyCode">Keys</param>
		/// <param name="virtualKeyCode">Keys</param>
		/// <returns>string</returns>
		public static string HotkeyToString(Keys modifierKeyCode, Keys virtualKeyCode)
		{
			return HotkeyModifiersToString(modifierKeyCode) + virtualKeyCode;
		}

		#region Native

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern int GetKeyNameText(uint lParam, [Out] StringBuilder lpString, int nSize);

		#endregion
	}
}