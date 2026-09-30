// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Enums;

namespace Dapplo.Windows.Input.Keyboard;

/// <summary>
/// Query the state of the keys of the keyboard (and the mouse buttons, which also have a <see cref="VirtualKeyCode"/>).
/// </summary>
/// <remarks>
/// Windows keeps two kinds of key state:
/// <list type="bullet">
/// <item><description>
/// The asynchronous state (GetAsyncKeyState), used by <see cref="IsDown"/> and <see cref="IsAnyDown"/>: the state of the key right now, system wide,
/// updated as soon as the input is processed, this includes input injected with SendInput.
/// Use this from background threads, in a hook, or when waiting until the user released some keys.
/// The async state is not updated for the key of the event which is currently being processed by a low-level hook, as the hook is called before the state changes.
/// It reads as "up" while another desktop (e.g. the secure desktop with UAC or Win+L) is active.
/// </description></item>
/// <item><description>
/// The state of the message queue of the calling thread (GetKeyState), used by <see cref="IsDownForCurrentThread"/> and <see cref="IsToggled"/>:
/// the state at the time of the keyboard message which the thread is currently processing.
/// Use this in a UI thread while handling a keyboard message, e.g. to check if Shift was down when the key which is being handled was pressed.
/// It lags behind the real state when the thread doesn't process keyboard messages (a thread without windows or a busy UI thread).
/// The toggle state (CapsLock, NumLock, ScrollLock) is only available in this state.
/// </description></item>
/// </list>
/// The generic keys <see cref="VirtualKeyCode.Shift"/>, <see cref="VirtualKeyCode.Control"/>, <see cref="VirtualKeyCode.Menu"/> and the pseudo key code
/// <see cref="VirtualKeyCode.Win"/> are down when the left or the right key is down.
/// </remarks>
public static class KeyboardState
{
    /// <summary>
    /// Check if the key is down right now (asynchronous, system wide state), see the remarks of <see cref="KeyboardState"/>.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode, a generic Shift, Control, Menu or Win checks the left and the right key</param>
    /// <returns>bool true if the key is down</returns>
    public static bool IsDown(VirtualKeyCode virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            VirtualKeyCode.Shift => IsAsyncDown(VirtualKeyCode.LeftShift) || IsAsyncDown(VirtualKeyCode.RightShift),
            VirtualKeyCode.Control => IsAsyncDown(VirtualKeyCode.LeftControl) || IsAsyncDown(VirtualKeyCode.RightControl),
            VirtualKeyCode.Menu => IsAsyncDown(VirtualKeyCode.LeftMenu) || IsAsyncDown(VirtualKeyCode.RightMenu),
            VirtualKeyCode.Win => IsAsyncDown(VirtualKeyCode.LeftWin) || IsAsyncDown(VirtualKeyCode.RightWin),
            _ => IsAsyncDown(virtualKeyCode)
        };
    }

    /// <summary>
    /// Check if any of the keys is down right now (asynchronous, system wide state), e.g. to wait until the user released the modifier keys before sending input.
    /// </summary>
    /// <param name="virtualKeyCodes">VirtualKeyCodes, a generic Shift, Control, Menu or Win checks the left and the right key</param>
    /// <returns>bool true if at least one of the keys is down</returns>
    public static bool IsAnyDown(params VirtualKeyCode[] virtualKeyCodes)
    {
        if (virtualKeyCodes is null) throw new ArgumentNullException(nameof(virtualKeyCodes));
        foreach (var virtualKeyCode in virtualKeyCodes)
        {
            if (IsDown(virtualKeyCode))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if the key was down at the time of the keyboard message which the calling thread is currently processing (GetKeyState),
    /// see the remarks of <see cref="KeyboardState"/>.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode, a generic Shift, Control, Menu or Win checks the left and the right key</param>
    /// <returns>bool true if the key is down</returns>
    public static bool IsDownForCurrentThread(VirtualKeyCode virtualKeyCode)
    {
        return virtualKeyCode switch
        {
            VirtualKeyCode.Shift => IsThreadDown(VirtualKeyCode.LeftShift) || IsThreadDown(VirtualKeyCode.RightShift),
            VirtualKeyCode.Control => IsThreadDown(VirtualKeyCode.LeftControl) || IsThreadDown(VirtualKeyCode.RightControl),
            VirtualKeyCode.Menu => IsThreadDown(VirtualKeyCode.LeftMenu) || IsThreadDown(VirtualKeyCode.RightMenu),
            VirtualKeyCode.Win => IsThreadDown(VirtualKeyCode.LeftWin) || IsThreadDown(VirtualKeyCode.RightWin),
            _ => IsThreadDown(virtualKeyCode)
        };
    }

    /// <summary>
    /// Check if a toggle key is on (the low bit of GetKeyState), e.g. <see cref="VirtualKeyCode.Capital"/> (CapsLock), <see cref="VirtualKeyCode.NumLock"/> or <see cref="VirtualKeyCode.Scroll"/>.
    /// This is the state of the message queue of the calling thread, see the remarks of <see cref="KeyboardState"/>.
    /// For other keys the bit flips with every press, which is rarely useful.
    /// </summary>
    /// <param name="virtualKeyCode">VirtualKeyCode</param>
    /// <returns>bool true if the toggle is on</returns>
    public static bool IsToggled(VirtualKeyCode virtualKeyCode)
    {
        return (GetKeyState(virtualKeyCode) & 0x0001) != 0;
    }

    private static bool IsAsyncDown(VirtualKeyCode virtualKeyCode) => (GetAsyncKeyState(virtualKeyCode) & 0x8000) != 0;

    private static bool IsThreadDown(VirtualKeyCode virtualKeyCode) => (GetKeyState(virtualKeyCode) & 0x8000) != 0;

    /// <summary>
    ///     Retrieve the asynchronous state of a key, see <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getasynckeystate">GetAsyncKeyState</a>
    /// </summary>
    /// <param name="keyCode">VirtualKeyCode</param>
    /// <returns>short, the high bit is set when the key is down</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetAsyncKeyState(VirtualKeyCode keyCode);

    /// <summary>
    ///     Retrieve the state of a key for the message queue of the calling thread, see <a href="https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getkeystate">GetKeyState</a>
    /// </summary>
    /// <param name="keyCode">VirtualKeyCode</param>
    /// <returns>short, the high bit is set when the key is down, the low bit when the key is toggled</returns>
    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern short GetKeyState(VirtualKeyCode keyCode);
}
