// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Advapi32;
using Dapplo.Windows.Citrix.Enums;
using Dapplo.Windows.Citrix.Structs;
using Dapplo.Windows.DesktopWindowsManager.Enums;
using Dapplo.Windows.Shell32;
using Dapplo.Windows.Shell32.Structs;
using Dapplo.Windows.SystemState;
using Dapplo.Windows.SystemState.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the fixes of the S4 sweep (Citrix, Shell32, DWM, SystemState, Advapi32)
/// </summary>
public class S4SweepTests
{
    [DllImport("user32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private static T FromNative<T>(params uint[] values) where T : struct
    {
        var memory = Marshal.AllocHGlobal(values.Length * sizeof(uint));
        try
        {
            Marshal.Copy(Array.ConvertAll(values, v => unchecked((int)v)), 0, memory, values.Length);
            return Marshal.PtrToStructure<T>(memory);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    [Fact]
    public void Citrix_EventMask_IsDword()
    {
        Assert.Equal(typeof(uint), Enum.GetUnderlyingType(typeof(EventMask)));
    }

    [Theory]
    [InlineData(1u, 4u)]
    [InlineData(2u, 8u)]
    [InlineData(4u, 16u)]
    [InlineData(8u, 24u)]
    [InlineData(16u, 15u)]
    [InlineData(24u, 32u)]
    public void Citrix_ClientDisplay_ColorDepth(uint nativeValue, uint bitsPerPixel)
    {
        var clientDisplay = FromNative<ClientDisplay>(1920, 1080, nativeValue);
        Assert.Equal(bitsPerPixel, clientDisplay.ColorDepth);
        Assert.Equal(1920, clientDisplay.ClientSize.Width);
        Assert.Equal(1080, clientDisplay.ClientSize.Height);
    }

    [Fact]
    public void Citrix_ClientLatency_Fields()
    {
        var clientLatency = FromNative<ClientLatency>(10, 20, 3);
        Assert.Equal(10u, clientLatency.Average);
        Assert.Equal(20u, clientLatency.Last);
        Assert.Equal(3u, clientLatency.Deviation);
    }

    [Fact]
    public void Citrix_SessionTime_IsLargeInteger()
    {
        Assert.Equal(5 * sizeof(long), Marshal.SizeOf<SessionTime>());
        var logonTime = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
        var fileTime = logonTime.ToFileTimeUtc();
        var memory = Marshal.AllocHGlobal(Marshal.SizeOf<SessionTime>());
        try
        {
            Marshal.Copy(new[] { fileTime, 0L, fileTime, fileTime, fileTime }, 0, memory, 5);
            var sessionTime = Marshal.PtrToStructure<SessionTime>(memory);
            Assert.Equal(logonTime, sessionTime.LogonTime);
            Assert.Equal(logonTime, sessionTime.ConnectTime);
            Assert.Null(sessionTime.DisconnectTime);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    [Fact]
    public void Shell32_AppBarData_HasNativeSize()
    {
        // APPBARDATA: cbSize, hWnd, uCallbackMessage, uEdge, RECT, LPARAM
        Assert.Equal(IntPtr.Size == 8 ? 48 : 36, Marshal.SizeOf<AppBarData>());
    }

    [Fact]
    public void Shell32_ExtractIconEx_MultipleIcons()
    {
        var shell32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");
        var count = Shell32Api.CountIcons(shell32);
        Assert.True(count > 2, $"Expected shell32.dll to contain icons, got {count}");

        var large = new IntPtr[3];
        var small = new IntPtr[3];
        var extracted = Shell32Api.ExtractIconEx(shell32, 0, large, small, 3);
        try
        {
            Assert.True(extracted >= 3, $"Expected 3 extracted icons, got {extracted}");
            Assert.All(large, handle => Assert.NotEqual(IntPtr.Zero, handle));
            Assert.All(small, handle => Assert.NotEqual(IntPtr.Zero, handle));
        }
        finally
        {
            foreach (var handle in large)
            {
                if (handle != IntPtr.Zero) DestroyIcon(handle);
            }
            foreach (var handle in small)
            {
                if (handle != IntPtr.Zero) DestroyIcon(handle);
            }
        }
    }

    [Fact]
    public void Dwm_WindowAttributes_Values()
    {
        Assert.Equal(2, (int)DwmWindowAttributes.NcRenderingPolicy);
        Assert.Equal(3, (int)DwmWindowAttributes.TransitionsForceDisabled);
        Assert.Equal(37, (int)DwmWindowAttributes.VisibleFrameBorderThickness);
        Assert.Equal(38, (int)DwmWindowAttributes.SystemBackdropType);
        Assert.Equal(39, (int)DwmWindowAttributes.Last);
    }

    [Fact]
    public void SystemState_PowerBroadcastEvent_OnlyDeliverableEvents()
    {
        Assert.False(Enum.IsDefined(typeof(PowerBroadcastEvent), 0x8013u));
        Assert.False(Enum.IsDefined(typeof(PowerBroadcastEvent), 0x0009u));
    }

    [Fact]
    public async Task SystemState_WaitableTimer_DisposeDuringWait()
    {
        var timer = new WaitableTimer();
        using var waitStarted = new ManualResetEventSlim();
        var waitTask = Task.Run(() =>
        {
            waitStarted.Set();
            try
            {
                return timer.Wait(TimeSpan.FromMilliseconds(500));
            }
            catch (ObjectDisposedException)
            {
                // Only when the Dispose won the race and happened before the wait started
                return false;
            }
        });
        waitStarted.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        timer.Dispose();
        // The wait keeps a reference on the handle, it is not closed under the wait, so the wait simply times out
        Assert.False(await waitTask);
        Assert.False(timer.IsValid);
        Assert.Throws<ObjectDisposedException>(() => timer.WaitHandle);
    }

    [Fact]
    public void SystemState_WaitableTimer_WaitHandle()
    {
        using var timer = new WaitableTimer();
        Assert.True(timer.SetOnce(TimeSpan.FromMilliseconds(50)));
        Assert.Equal(0, WaitHandle.WaitAny(new[] { timer.WaitHandle }, TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void Advapi32_CurrentLogonSid()
    {
        var logonSid = Advapi32Api.CurrentLogonSid;
        Assert.StartsWith("S-1-5-5-", logonSid);
    }
}
