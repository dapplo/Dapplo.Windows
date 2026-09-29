// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.User32.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Pure logic tests for the User32 interop definitions, these do not touch the desktop
/// </summary>
public class User32InteropTests
{
    [Fact]
    public void MonitorFrom_MatchesWinUser()
    {
        // MONITOR_DEFAULTTONULL, MONITOR_DEFAULTTOPRIMARY, MONITOR_DEFAULTTONEAREST
        Assert.Equal(0u, (uint)MonitorFrom.DefaultToNull);
        Assert.Equal(1u, (uint)MonitorFrom.DefaultToPrimary);
        Assert.Equal(2u, (uint)MonitorFrom.DefaultToNearest);
        Assert.False(Attribute.IsDefined(typeof(MonitorFrom), typeof(FlagsAttribute)));
    }

    [Fact]
    public void SysColorIndexes_Color3Dface_IsBtnFace()
    {
        // COLOR_3DFACE == COLOR_BTNFACE == 15
        Assert.Equal(15, (int)SysColorIndexes.Color3Dface);
        Assert.Equal(SysColorIndexes.ColorBtnface, SysColorIndexes.Color3Dface);
    }

    [Fact]
    public void ScrollBarInfo_Layout_MatchesNative()
    {
        // SCROLLBARINFO: cbSize, rcScrollBar, dxyLineButton, xyThumbTop, xyThumbBottom, reserved, rgstate[6]
        Assert.Equal(60, Marshal.SizeOf(typeof(ScrollBarInfo)));
        Assert.Equal(20, Marshal.OffsetOf(typeof(ScrollBarInfo), "_dxyLineButton").ToInt32());
        Assert.Equal(24, Marshal.OffsetOf(typeof(ScrollBarInfo), "_thumbTop").ToInt32());
        Assert.Equal(28, Marshal.OffsetOf(typeof(ScrollBarInfo), "_thumbBottom").ToInt32());
    }

    [Fact]
    public void CreateScrollWParam_PutsCommandInLowWordAndPositionInHighWord()
    {
        var wParam = WindowScroller.CreateScrollWParam(ScrollBarCommands.SB_THUMBPOSITION, 5);
        Assert.Equal(0x00050004u, unchecked((uint)wParam.ToInt64()));
    }

    [Fact]
    public void CreateScrollWParam_LargePosition_DoesNotOverflow()
    {
        // 40000 << 16 sets the high bit of the 32-bit value, this must not throw (also not on 32-bit)
        var wParam = WindowScroller.CreateScrollWParam(ScrollBarCommands.SB_THUMBPOSITION, 40000);
        Assert.Equal((40000u << 16) | 4u, unchecked((uint)wParam.ToInt64()));
    }
}
