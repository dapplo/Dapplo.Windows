// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Windows.Forms;
using BenchmarkDotNet.Attributes;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;

namespace Dapplo.Windows.Tests.Benchmarks;

[MinColumn, MaxColumn, MemoryDiagnoser]
public class ScreenboundsBenchmark : IDisposable
{
    [GlobalSetup]
    public void Setup()
    {
        var screenbounds = GetAllScreenBounds();
    }

    [Benchmark]
    public void ScreenBoundsNative()
    {
        var screenbounds = GetScreenBounds();
    }

    [Benchmark]
    public void ScreenBoundsPInvoke()
    {
        var screenbounds = GetAllScreenBounds();
    }

    [Benchmark]
    public void ScreenBoundsPInvokeCached()
    {
        var screenbounds = DisplayInfo.ScreenBounds;
    }

    /// <summary>
    ///     Get the bounds of the complete screen
    /// </summary>
    /// <returns></returns>
    public NativeRect GetAllScreenBounds()
    {
        int left = 0, top = 0, bottom = 0, right = 0;
        foreach (var display in DisplayInfo.AllDisplayInfos)
        {
            var currentBounds = display.Bounds;
            left = Math.Min(left, currentBounds.X);
            top = Math.Min(top, currentBounds.Y);
            var screenAbsRight = currentBounds.X + currentBounds.Width;
            var screenAbsBottom = currentBounds.Y + currentBounds.Height;
            right = Math.Max(right, screenAbsRight);
            bottom = Math.Max(bottom, screenAbsBottom);
        }
        return new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
    }

    /// <summary>
    ///     Get the bounds of all screens combined.
    /// </summary>
    /// <returns>A NativeRect of the bounds of the entire display area.</returns>
    public NativeRect GetScreenBounds()
    {
        int left = 0, top = 0, bottom = 0, right = 0;
        foreach (var screen in Screen.AllScreens)
        {
            left = Math.Min(left, screen.Bounds.X);
            top = Math.Min(top, screen.Bounds.Y);
            var screenAbsRight = screen.Bounds.X + screen.Bounds.Width;
            var screenAbsBottom = screen.Bounds.Y + screen.Bounds.Height;
            right = Math.Max(right, screenAbsRight);
            bottom = Math.Max(bottom, screenAbsBottom);
        }
        return new NativeRect(left, top, right + Math.Abs(left), bottom + Math.Abs(top));
    }

    [GlobalCleanup]
    public void Dispose()
    {
    }
}