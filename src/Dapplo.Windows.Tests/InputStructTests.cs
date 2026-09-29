// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the SendInput structures
/// </summary>
public class InputStructTests
{
    [Fact]
    public void TestNormalizeCoordinate_PrimaryOnly()
    {
        Assert.Equal(0, MouseInput.NormalizeCoordinate(0, 0, 1920));
        Assert.Equal(65535, MouseInput.NormalizeCoordinate(1919, 0, 1920));
        // 100 * 65535 / 1919 = 3415.06
        Assert.Equal(3415, MouseInput.NormalizeCoordinate(100, 0, 1920));
    }

    [Fact]
    public void TestNormalizeCoordinate_VirtualDesktopOrigin()
    {
        // A monitor left of the primary: virtual desktop from -1920 to 1919
        Assert.Equal(0, MouseInput.NormalizeCoordinate(-1920, -1920, 3840));
        Assert.Equal(65535, MouseInput.NormalizeCoordinate(1919, -1920, 3840));
        // 0,0 of the primary is in the middle
        Assert.Equal(32776, MouseInput.NormalizeCoordinate(0, -1920, 3840));
    }

    [Fact]
    public void TestNormalizeCoordinate_Clamped()
    {
        Assert.Equal(0, MouseInput.NormalizeCoordinate(-100, 0, 1920));
        Assert.Equal(65535, MouseInput.NormalizeCoordinate(10000, 0, 1920));
        Assert.Equal(0, MouseInput.NormalizeCoordinate(100, 0, 0));
    }

    [Fact]
    public void TestIsExtendedKey()
    {
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.Right));
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.Delete));
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.RightControl));
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.RightMenu));
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.LeftWin));
        Assert.True(KeyboardInput.IsExtendedKey(VirtualKeyCode.VolumeUp));
        Assert.False(KeyboardInput.IsExtendedKey(VirtualKeyCode.LeftControl));
        Assert.False(KeyboardInput.IsExtendedKey(VirtualKeyCode.KeyA));
        Assert.False(KeyboardInput.IsExtendedKey(VirtualKeyCode.Shift));
    }

    [Fact]
    public void TestKeyboardInput_Flags()
    {
        var down = KeyboardInput.ForKeyDown(VirtualKeyCode.Right);
        Assert.Equal(KeyEventFlags.ExtendedKey, down.KeyEventFlags);
        Assert.Equal(0u, down.Timestamp);

        var up = KeyboardInput.ForKeyUp(VirtualKeyCode.Right);
        Assert.Equal(KeyEventFlags.ExtendedKey | KeyEventFlags.KeyUp, up.KeyEventFlags);

        var normalUp = KeyboardInput.ForKeyUp(VirtualKeyCode.KeyA, 1234);
        Assert.Equal(KeyEventFlags.KeyUp, normalUp.KeyEventFlags);
        Assert.Equal(1234u, normalUp.Timestamp);
    }
}
