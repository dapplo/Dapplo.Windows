﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input;

/// <summary>
/// Native input methods
/// </summary>
public static class NativeInput
{
    /// <summary>
    ///     Wrapper to simplify sending of inputs
    /// </summary>
    /// <param name="inputs">Input array</param>
    /// <returns>inputs send</returns>
    public static uint SendInput(Structs.Input[] inputs)
    {
        return SendInput((uint)inputs.Length, inputs, Structs.Input.Size);
    }

    /// <summary>
    /// Get a DateTimeOffset which specifies the last input timestamp
    /// </summary>
    /// <returns>DateTimeOffset</returns>
    public static DateTimeOffset LastInputDateTime
    {
        get
        {
            var lastInputInfo = LastInputInfo.Create();
            if (GetLastInputInfo(ref lastInputInfo))
            {
                return lastInputInfo.LastInputDateTime;
            }

            return DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Get a TimeSpan which specifies how long ago the last input was
    /// </summary>
    /// <returns>TimeSpan</returns>
    public static TimeSpan LastInputTimeSpan
    {
        get
        {
            var lastInputInfo = LastInputInfo.Create();
            if (GetLastInputInfo(ref lastInputInfo))
            {
                return lastInputInfo.LastInputTimeSpan;
            }

            return TimeSpan.MaxValue;
        }
    }

    /// <summary>
    /// Get the last input info, see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms646302(v=vs.85).aspx">GetLastInputInfo function</a>
    /// </summary>
    /// <param name="lastInputInfo">LastInputInfo</param>
    /// <returns>bool</returns>
    [DllImport("User32")]
    private static extern bool GetLastInputInfo(ref LastInputInfo lastInputInfo);

    /// <summary>
    ///     Synthesizes keystrokes, mouse motions, and button clicks.
    ///     The function returns the number of events that it successfully inserted into the keyboard or mouse input stream.
    ///     If the function returns zero, the input was already blocked by another thread.
    ///     To get extended error information, call GetLastError.
    /// </summary>
    [DllImport("user32", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray)] [In] Structs.Input[] inputs, int cbSize);
}