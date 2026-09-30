// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Desktop;
using Xunit;

namespace Dapplo.Windows.Tests;

public class WindowScrollerTests
{
    [Fact]
    public void CalculateWheelDelta_ScrollsOnePage()
    {
        // 30 lines per page, 3 lines per notch = 10 notches
        Assert.Equal(1200, WindowScroller.CalculateWheelDelta(30, 3));
    }

    [Fact]
    public void CalculateWheelDelta_NoScrollLines_DoesNotThrow()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(30, 0));
    }

    [Fact]
    public void CalculateWheelDelta_PageScroll_IsOneNotch()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(30, uint.MaxValue));
    }

    [Fact]
    public void CalculateWheelDelta_SmallPage_IsAtLeastOneNotch()
    {
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(2, 3));
        Assert.Equal(WindowScroller.WheelDeltaPerNotch, WindowScroller.CalculateWheelDelta(0, 3));
    }

    [Fact]
    public void CalculateWheelDelta_HugePage_DoesNotOverflow()
    {
        Assert.Equal(int.MaxValue, WindowScroller.CalculateWheelDelta(uint.MaxValue - 1, 1));
    }
}
