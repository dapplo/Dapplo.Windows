// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// The name of the application which blocks the clipboard, for implementations of IClipboardAccessToken which aren't the library's.
/// These tests don't use the clipboard, see ClipboardSnapshotTests for the library's tokens and the exception.
/// </summary>
public class ClipboardBlockerTests
{
    internal static string CurrentExecutableFileName
    {
        get
        {
            using var process = Process.GetCurrentProcess();
            return Path.GetFileName(process.MainModule!.FileName);
        }
    }

    [Fact]
    public void ForeignToken_WithProcess_IsTheExecutableFileName()
    {
        using var process = Process.GetCurrentProcess();
        var token = new ForeignToken { BlockingProcessId = process.Id };
        Assert.Equal(CurrentExecutableFileName, token.GetBlockingProcessName());
    }

    [Fact]
    public void ForeignToken_WithoutProcess_IsTheWindowTitle()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams { Caption = "Dapplo clipboard blocker test" });
        try
        {
            // No process ID, or one which doesn't exist: the title of the window
            Assert.Equal("Dapplo clipboard blocker test", new ForeignToken { BlockingWindow = window.Handle }.GetBlockingProcessName());
            Assert.Equal("Dapplo clipboard blocker test", new ForeignToken { BlockingWindow = window.Handle, BlockingProcessId = int.MaxValue - 3 }.GetBlockingProcessName());
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    [Fact]
    public void ForeignToken_Unknown_IsNull()
    {
        Assert.Null(new ForeignToken().GetBlockingProcessName());
        Assert.Null(new ForeignToken { BlockingProcessId = int.MaxValue - 3 }.GetBlockingProcessName());
        Assert.Throws<ArgumentNullException>(() => ((IClipboardAccessToken)null).GetBlockingProcessName());
    }

    /// <summary>
    /// An IClipboardAccessToken which isn't the library's, e.g. a test double of an application
    /// </summary>
    private sealed class ForeignToken : IClipboardAccessToken
    {
        public bool CanAccess => false;
        public bool IsLockTimeout => false;
        public bool IsOpenTimeout => true;
        public IntPtr BlockingWindow { get; set; }
        public int BlockingProcessId { get; set; }
        public void ThrowWhenNoAccess() => throw new ClipboardAccessDeniedException("Test");
        public void Dispose()
        {
        }
    }
}

/// <summary>
/// ClipboardNative.AvailableFormats without the clipboard content, see ClipboardSnapshotTests for the interactive tests
/// </summary>
public class ClipboardAvailableFormatsTests
{
    [Fact]
    public void AvailableFormats_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ClipboardNative.AvailableFormats(null, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ClipboardNative.AvailableFormats(new[] { "CF_TEXT" }, -1));
    }
}
