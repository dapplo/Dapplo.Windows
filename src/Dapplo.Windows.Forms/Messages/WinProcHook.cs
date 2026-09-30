// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Forms.Messages;

/// <summary>
///     A hook for the window procedure of a <see cref="WinProcListener"/>, it has the same signature as the WPF HwndSourceHook.
/// </summary>
/// <param name="hWnd">IntPtr with the window handle</param>
/// <param name="msg">int with the message</param>
/// <param name="wParam">IntPtr with the wParam</param>
/// <param name="lParam">IntPtr with the lParam</param>
/// <param name="handled">set to true to stop the processing of the message, the return value is then the result for Windows</param>
/// <returns>IntPtr with the result of the message, only used when handled is set to true</returns>
public delegate IntPtr WinProcHook(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled);
