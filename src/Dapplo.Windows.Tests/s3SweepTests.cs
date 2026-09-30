// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Com;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.Kernel32;
using Dapplo.Windows.Kernel32.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the sweep (pass 5) fixes of Dpi, Kernel32, Com and Clipboard
/// </summary>
public class S3SweepTests
{
    private sealed class TrackedValue : IDisposable
    {
        public TrackedValue(int dpi)
        {
            Dpi = dpi;
        }

        public int Dpi { get; }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private static WindowMessage CreateDpiChanged(int dpi) =>
        new WindowMessage(IntPtr.Zero, WindowsMessages.WM_DPICHANGED, new IntPtr((dpi << 16) | dpi), IntPtr.Zero);

    /// <summary>
    ///     D-32: scaling rounds (like MulDiv) instead of truncating
    /// </summary>
    [Fact]
    public void DpiCalculator_Rounds()
    {
        Assert.Equal(5, DpiCalculator.ScaleWithDpi(3, 144));
        Assert.Equal(new NativeSize(5, 5), DpiCalculator.ScaleWithDpi(new NativeSize(3, 3), 144));
        Assert.Equal(new NativePoint(5, 5), DpiCalculator.ScaleWithDpi(new NativePoint(3, 3), 144));
        Assert.Equal(13, DpiCalculator.UnscaleWithDpi(16, 120));
        Assert.Equal(4.5f, DpiCalculator.ScaleWithDpi(new NativePointFloat(3f, 3f), 144).X);
        Assert.Equal(2f, DpiCalculator.UnscaleWithDpi(new NativePointFloat(3f, 3f), 144).X, 3);
    }

    /// <summary>
    ///     D-32: an unknown DPI (0) doesn't give 0 or Infinity
    /// </summary>
    [Fact]
    public void DpiCalculator_UnknownDpi_IsDefault()
    {
        Assert.Equal(16, DpiCalculator.ScaleWithDpi(16, 0));
        Assert.Equal(16, DpiCalculator.UnscaleWithDpi(16, 0));
        var dpiHandler = new DpiHandler();
        Assert.False(dpiHandler.IsDpiKnown);
        Assert.Equal(DpiCalculator.DefaultScreenDpi, dpiHandler.Dpi);
        Assert.Equal(16, dpiHandler.ScaleWithCurrentDpi(16));
        Assert.Equal(16, dpiHandler.UnscaleWithCurrentDpi(16));
    }

    /// <summary>
    ///     D-20: WM_DESTROY (e.g. a handle recreate) doesn't complete the DpiHandler, Dispose does
    /// </summary>
    [Fact]
    public void DpiHandler_SurvivesDestroy_CompletesOnDispose()
    {
        var dpiHandler = new DpiHandler
        {
            ApplySuggestedWindowRect = false
        };
        var changes = new List<DpiChangeInfo>();
        var completed = false;
        using var subscription = dpiHandler.OnDpiChanged.Subscribe(changes.Add, () => completed = true);

        Assert.False(dpiHandler.HandleWindowMessages(CreateDpiChanged(144)));
        Assert.True(dpiHandler.IsDpiKnown);
        Assert.Equal(144, dpiHandler.Dpi);

        dpiHandler.HandleWindowMessages(new WindowMessage(IntPtr.Zero, WindowsMessages.WM_DESTROY, IntPtr.Zero, IntPtr.Zero));
        Assert.False(completed);

        dpiHandler.HandleWindowMessages(CreateDpiChanged(192));
        Assert.Equal(2, changes.Count);
        Assert.Equal(0, changes[0].PreviousDpi);
        Assert.Equal(144, changes[0].NewDpi);
        Assert.Equal(144, changes[1].PreviousDpi);
        Assert.Equal(192, changes[1].NewDpi);

        dpiHandler.Dispose();
        Assert.True(completed);
        // A second dispose is fine
        dpiHandler.Dispose();
    }

    /// <summary>
    ///     D-23: the bitmaps are cached, a replaced original is disposed and the cache is disposed on a DPI change and dispose
    /// </summary>
    [Fact]
    public void BitmapScaleHandler_CachesAndDisposes()
    {
        var dpiHandler = new DpiHandler
        {
            ApplySuggestedWindowRect = false
        };
        var provided = new List<TrackedValue>();
        var scaled = new List<TrackedValue>();
        var handler = BitmapScaleHandler.Create<string, TrackedValue>(dpiHandler, (key, dpi) =>
        {
            var value = new TrackedValue(dpi);
            provided.Add(value);
            return value;
        }, (value, dpi) =>
        {
            if (dpi == DpiCalculator.DefaultScreenDpi)
            {
                return value;
            }
            var result = new TrackedValue(dpi);
            scaled.Add(result);
            return result;
        });

        var applied = new List<TrackedValue>();
        handler.AddTargetAction(applied, "image", applied.Add, true);
        handler.AddTargetAction(provided, "image", _ => { }, true);
        // Only one provider call for the same key and DPI
        Assert.Single(provided);
        Assert.Equal(DpiCalculator.DefaultScreenDpi, applied[0].Dpi);

        dpiHandler.HandleWindowMessages(CreateDpiChanged(144));
        Assert.Equal(2, provided.Count);
        // The 96 DPI bitmap was cached, and is disposed now
        Assert.True(provided[0].IsDisposed);
        // The original of the scaled bitmap is disposed, the scaled one is in use
        Assert.True(provided[1].IsDisposed);
        Assert.Single(scaled);
        Assert.False(scaled[0].IsDisposed);
        Assert.Same(scaled[0], applied[1]);

        handler.Dispose();
        Assert.True(scaled[0].IsDisposed);
        dpiHandler.Dispose();
    }

    /// <summary>
    ///     D-39: enums with sequential values are no flags
    /// </summary>
    [Fact]
    public void Enums_AreNoFlags()
    {
        Assert.False(typeof(MonitorDpiType).IsDefined(typeof(FlagsAttribute), false));
        Assert.False(typeof(WindowsProductTypes).IsDefined(typeof(FlagsAttribute), false));
    }

    /// <summary>
    ///     D-25: the process path is retrieved, and the device path conversion gives the same result
    /// </summary>
    [Fact]
    public void Kernel32_GetProcessPath()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var expected = currentProcess.MainModule?.FileName;
        Assert.NotNull(expected);
        Assert.Equal(expected, Kernel32Api.GetProcessPath(currentProcess.Id), StringComparer.OrdinalIgnoreCase);

        var hProcess = Kernel32Api.OpenProcess(ProcessAccessRights.QueryLimitedInformation, false, currentProcess.Id);
        Assert.NotEqual(IntPtr.Zero, hProcess);
        try
        {
            var devicePath = PsApi.GetProcessImageFileName(hProcess);
            Assert.StartsWith("\\Device\\", devicePath, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(expected, Kernel32Api.DevicePathToDosPath(devicePath), StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            Kernel32Api.CloseHandle(hProcess);
        }

        var systemDrive = Environment.GetFolderPath(Environment.SpecialFolder.System).Substring(0, 2);
        var dosDevice = Kernel32Api.QueryDosDevice(systemDrive);
        Assert.NotNull(dosDevice);
        // Ordinal check, a culture sensitive string search (ICU) ignores NUL
        Assert.Equal(-1, dosDevice.IndexOf('\0'));
        Assert.StartsWith("\\Device\\", dosDevice, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     D-30: without package identity the full name is null
    /// </summary>
    [Fact]
    public void PackageInfo_NoPackage()
    {
        Assert.Null(PackageInfo.CurrentPackageFullName);
        Assert.False(PackageInfo.HasPackageIdentity);
    }

    /// <summary>
    ///     D-36: an unknown ProgID gives null instead of Guid.Empty
    /// </summary>
    [Fact]
    public void Ole32Api_UnknownProgId_IsNull()
    {
        Assert.Null(Ole32Api.ClassIdFromProgId("Dapplo.Windows.DoesNotExist.12345"));
        Assert.Null(OleAut32Api.GetActiveObject<object>("Dapplo.Windows.DoesNotExist.12345"));
    }

    /// <summary>
    ///     D-34: the clipboard stream is a copy, which is still readable after the access token is disposed
    /// </summary>
    [WpfFact]
    [Trait("Category", "Interactive")]
    public async Task Clipboard_Stream_OutlivesAccessToken()
    {
        const string testString = "Dapplo.Windows.Tests.S3SweepTests";
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            clipboardAccessToken.ClearContents();
            clipboardAccessToken.SetAsUnicodeString(testString);
        }
        await Task.Delay(100);

        Stream stream;
        using (var clipboardAccessToken = await ClipboardNative.AccessAsync())
        {
            Assert.True(clipboardAccessToken.CanAccess);
            stream = clipboardAccessToken.GetAsStream(StandardClipboardFormats.UnicodeText);
            Assert.True(clipboardAccessToken.TryGetAsStream(StandardClipboardFormats.UnicodeText, out var tryStream));
            tryStream.Dispose();
        }

        using (stream)
        {
            Assert.False(stream.CanWrite);
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            Assert.Equal(testString, Encoding.Unicode.GetString(memoryStream.ToArray()).TrimEnd('\0'));
        }
    }
}
