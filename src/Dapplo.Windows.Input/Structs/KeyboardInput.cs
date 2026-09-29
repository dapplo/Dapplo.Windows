// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Structs;

/// <summary>
///     This struct contains information about a simulated keyboard event.
///     See
///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646271.aspx">KEYBDINPUT structure</a>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct KeyboardInput
{
    private VirtualKeyCode _wVk;
    private ScanCodes _wScan;
    private KeyEventFlags _dwFlags;
    private uint _time;
    private UIntPtr _dwExtraInfo;

    /// <summary>
    ///     A virtual-key code. The code must be a value in the range 1 to 254.
    ///     If the dwFlags member specifies KEYEVENTF_UNICODE, wVk must be 0.
    /// </summary>
    public VirtualKeyCode VirtualKeyCode
    {
        get { return _wVk; }
        set { _wVk = value; }
    }

    /// <summary>
    ///     A hardware scan code for the key. If KeyEventFlags specifies Unicode, ScanCode specifies a Unicode character which
    ///     is to be sent to the foreground application.
    /// </summary>
    public ScanCodes ScanCode
    {
        get { return _wScan; }
        set { _wScan = value; }
    }

    /// <summary>
    ///     Specifies various aspects of a keystroke. This member can be certain combinations of the following values.
    /// </summary>
    public KeyEventFlags KeyEventFlags
    {
        get { return _dwFlags; }
        set { _dwFlags = value; }
    }

    /// <summary>
    ///     The time stamp for the event, in milliseconds. If this parameter is zero, the system will provide its own time
    ///     stamp.
    /// </summary>
    public uint Timestamp
    {
        get { return _time; }
        set { _time = value; }
    }

    /// <summary>
    ///     An additional value associated with the keystroke. Use the GetMessageExtraInfo function to obtain this information.
    /// </summary>
    public UIntPtr ExtraInfo
    {
        get { return _dwExtraInfo; }
        set { _dwExtraInfo = value; }
    }

    /// <summary>
    ///     Create a KeyboardInput for a key press (up / down)
    /// </summary>
    /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
    /// <param name="timestamp">optional Timestamp, null or 0 lets the system provide the time stamp</param>
    /// <returns>KeyboardInput[]</returns>
    public static KeyboardInput[] ForKeyPress(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
    {
        return new[]
        {
            ForKeyDown(virtualKeyCode, timestamp),
            ForKeyUp(virtualKeyCode, timestamp)
        };
    }

    /// <summary>
    ///     Create a KeyboardInput for a key down
    /// </summary>
    /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
    /// <param name="timestamp">optional Timestamp, null or 0 lets the system provide the time stamp</param>
    /// <returns>KeyboardInput</returns>
    public static KeyboardInput ForKeyDown(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
    {
        // The generic Win is a pseudo key code, send the left Windows key
        if (virtualKeyCode == VirtualKeyCode.Win)
        {
            virtualKeyCode = VirtualKeyCode.LeftWin;
        }
        return new KeyboardInput
        {
            VirtualKeyCode = virtualKeyCode,
            ScanCode = MapToScanCode(virtualKeyCode),
            KeyEventFlags = IsExtendedKey(virtualKeyCode) ? KeyEventFlags.ExtendedKey : 0,
            Timestamp = timestamp ?? 0
        };
    }

    /// <summary>
    ///     Create a KeyboardInput for a key up
    /// </summary>
    /// <param name="virtualKeyCode">Value from VirtualKeyCodes</param>
    /// <param name="timestamp">optional Timestamp, null or 0 lets the system provide the time stamp</param>
    /// <returns>KeyboardInput</returns>
    public static KeyboardInput ForKeyUp(VirtualKeyCode virtualKeyCode, uint? timestamp = null)
    {
        // The generic Win is a pseudo key code, send the left Windows key
        if (virtualKeyCode == VirtualKeyCode.Win)
        {
            virtualKeyCode = VirtualKeyCode.LeftWin;
        }
        var keyEventFlags = KeyEventFlags.KeyUp;
        if (IsExtendedKey(virtualKeyCode))
        {
            keyEventFlags |= KeyEventFlags.ExtendedKey;
        }
        return new KeyboardInput
        {
            VirtualKeyCode = virtualKeyCode,
            ScanCode = MapToScanCode(virtualKeyCode),
            KeyEventFlags = keyEventFlags,
            Timestamp = timestamp ?? 0
        };
    }

    /// <summary>
    ///     Check if the specified VirtualKeyCode is an extended key (E0 prefixed scan code), these need KEYEVENTF_EXTENDEDKEY
    ///     when they are injected, otherwise Windows uses the non-extended (numpad) equivalent.
    ///     Examples are the arrow keys, Insert, Delete, Home, End, PageUp, PageDown, RightControl, RightMenu (AltGr), Divide, NumLock, the Windows keys and the media keys.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool true if the key is an extended key</returns>
    public static bool IsExtendedKey(VirtualKeyCode virtualKeyCode)
    {
        switch (virtualKeyCode)
        {
            case VirtualKeyCode.Cancel:
            case VirtualKeyCode.Prior:
            case VirtualKeyCode.Next:
            case VirtualKeyCode.End:
            case VirtualKeyCode.Home:
            case VirtualKeyCode.Left:
            case VirtualKeyCode.Up:
            case VirtualKeyCode.Right:
            case VirtualKeyCode.Down:
            case VirtualKeyCode.Snapshot:
            case VirtualKeyCode.Insert:
            case VirtualKeyCode.Delete:
            case VirtualKeyCode.LeftWin:
            case VirtualKeyCode.RightWin:
            case VirtualKeyCode.Win:
            case VirtualKeyCode.Apps:
            case VirtualKeyCode.Sleep:
            case VirtualKeyCode.Divide:
            case VirtualKeyCode.NumLock:
            case VirtualKeyCode.RightControl:
            case VirtualKeyCode.RightMenu:
                return true;
        }
        // Browser, volume, media and launch keys
        return virtualKeyCode >= VirtualKeyCode.BrowserBack && virtualKeyCode <= VirtualKeyCode.LaunchApp2;
    }

    /// <summary>
    ///     Map the VirtualKeyCode to the scan code of the current keyboard layout, so hooks and raw input see a correct scan code
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>ScanCodes</returns>
    private static ScanCodes MapToScanCode(VirtualKeyCode virtualKeyCode)
    {
        var scanCode = MapVirtualKey((uint)virtualKeyCode, (uint)MapVkType.VkToVsc);
        return (ScanCodes)(short)(scanCode & 0xFF);
    }

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);
}