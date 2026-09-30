// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// Tests for the blocking window diagnostics, ClipboardSnapshot and IClipboardDataSource
/// </summary>
/// <remarks>Interactive: these tests change the clipboard. They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class ClipboardSnapshotTests
{
    private const string JsonFormat = "Dapplo.Windows.Tests.ClipboardSnapshotTests.Json";
    private const string LargeFormat = "Dapplo.Windows.Tests.ClipboardSnapshotTests.Large";

    private static NativeWindow CreateWindow()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams());
        return window;
    }

    // ── B1: who blocks the clipboard ──────────────────────────────────────────

    [WpfFact]
    public async Task UseAsync_Blocked_ExceptionNamesTheBlockingWindowAndProcess()
    {
        var window = CreateWindow();
        try
        {
            var blocked = ClipboardThreadingTests.BlockClipboard(TimeSpan.FromMilliseconds(500), window.Handle);
            Assert.Equal(window.Handle, ClipboardNative.OpenClipboardWindow);

            var exception = await Assert.ThrowsAsync<ClipboardAccessDeniedException>(() =>
                ClipboardNative.UseAsync(_ => { }, new ClipboardAccessOptions { Retries = 1, RetryInterval = TimeSpan.FromMilliseconds(20) }));

            Assert.True(exception.IsOpenTimeout);
            Assert.False(exception.IsLockTimeout);
            Assert.Equal(window.Handle, exception.BlockingWindow);
            Assert.Equal(Process.GetCurrentProcess().Id, exception.BlockingProcessId);
            Assert.Contains(Process.GetCurrentProcess().ProcessName, exception.Message);
            await blocked;
            Assert.Equal(IntPtr.Zero, ClipboardNative.OpenClipboardWindow);
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    [WpfFact]
    public async Task Access_Blocked_TokenNamesTheBlockingWindow()
    {
        var window = CreateWindow();
        try
        {
            var blocked = ClipboardThreadingTests.BlockClipboard(TimeSpan.FromMilliseconds(500), window.Handle);
            using (var clipboard = ClipboardNative.Access(retries: 1, retryInterval: TimeSpan.FromMilliseconds(20)))
            {
                Assert.False(clipboard.CanAccess);
                Assert.True(clipboard.IsOpenTimeout);
                Assert.Equal(window.Handle, clipboard.BlockingWindow);
                Assert.Equal(Process.GetCurrentProcess().Id, clipboard.BlockingProcessId);
                var exception = Assert.Throws<ClipboardAccessDeniedException>(() => clipboard.ThrowWhenNoAccess());
                Assert.Equal(window.Handle, exception.BlockingWindow);
                Assert.True(exception.IsOpenTimeout);
            }
            await blocked;
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    [WpfFact]
    public async Task LockTimeout_Exception_IsLockTimeout()
    {
        using (ClipboardNative.Access())
        {
            // The in-process lock is held by this thread, UseAsync can't get it
            var exception = await Assert.ThrowsAsync<ClipboardAccessDeniedException>(() =>
                ClipboardNative.UseAsync(_ => { }, new ClipboardAccessOptions { LockTimeout = TimeSpan.FromMilliseconds(50) }));
            Assert.True(exception.IsLockTimeout);
            Assert.Equal(IntPtr.Zero, exception.BlockingWindow);
        }
    }

    // ── B2: snapshot ──────────────────────────────────────────────────────────

    private static async Task WriteTestContent(byte[] large = null)
    {
        var contents = new ClipboardContents()
            .AddUnicodeString("Snapshot text ✓")
            .AddBytes(Encoding.UTF8.GetBytes("{\"a\":1}"), JsonFormat);
        if (large != null)
        {
            contents.AddBytes(large, LargeFormat);
        }
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
    }

    [WpfFact]
    public async Task Snapshot_AllFormats_ContainsTheData_AndSurvivesAClipboardChange()
    {
        await WriteTestContent();

        var snapshot = await ClipboardNative.ReadSnapshotAsync();

        Assert.Equal(ClipboardNative.SequenceNumber, snapshot.SequenceNumber);
        Assert.Contains("CF_UNICODETEXT", snapshot.Formats);
        Assert.Contains(JsonFormat, snapshot.Formats);
        // Windows synthesizes CF_TEXT from CF_UNICODETEXT, it's global memory and is copied too
        Assert.Contains("CF_TEXT", snapshot.Formats);
        Assert.True(snapshot.HasFormat(JsonFormat.ToUpperInvariant()), "Format names are case-insensitive, like registered clipboard formats");

        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Changed")));

        Assert.Equal("Snapshot text ✓", snapshot.GetAsUnicodeString());
        Assert.Equal("{\"a\":1}", snapshot.TryGetAsUtf8String(JsonFormat, out var json) ? json : null);
        Assert.NotEqual(ClipboardNative.SequenceNumber, snapshot.SequenceNumber);
    }

    [WpfFact]
    public async Task Snapshot_RequestedFormats_OnlyThoseInClipboardOrder()
    {
        await WriteTestContent();

        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { JsonFormat, "CF_UNICODETEXT", "Dapplo.Windows.Tests.NotOnTheClipboard" });

        Assert.Equal(new[] { "CF_UNICODETEXT", JsonFormat }, snapshot.Formats.ToArray());
        Assert.Empty(snapshot.SkippedFormats);
        Assert.False(snapshot.HasFormat("CF_TEXT"));
    }

    [WpfFact]
    public async Task Snapshot_SizeLimit_SkipsLargeFormats()
    {
        var large = new byte[64 * 1024];
        new Random(42).NextBytes(large);
        await WriteTestContent(large);

        var limited = await ClipboardNative.ReadSnapshotAsync(null, 1024);
        Assert.Contains(LargeFormat, limited.SkippedFormats);
        Assert.False(limited.HasFormat(LargeFormat));
        Assert.True(limited.HasFormat(JsonFormat));

        var full = await ClipboardNative.ReadSnapshotAsync(new[] { LargeFormat });
        Assert.True(full.TryGetAsBytes(LargeFormat, out var bytes));
        // The allocation can be rounded up, the content starts with our bytes
        Assert.True(bytes.Length >= large.Length);
        Assert.Equal(large, bytes.Take(large.Length).ToArray());
        Assert.True(full.GetSize(LargeFormat) >= large.Length);
    }

    [WpfFact]
    public async Task Snapshot_HandleFormats_AreSkipped()
    {
        var window = CreateWindow();
        try
        {
            // A bitmap placed as CF_BITMAP handle: Windows synthesizes CF_DIB and CF_DIBV5, which are global memory
            using var bitmap = new System.Drawing.Bitmap(4, 3);
            var hBitmap = bitmap.GetHbitmap();
            using (var clipboard = ClipboardNative.Access(window.Handle))
            {
                clipboard.ClearContents();
                Assert.NotEqual(IntPtr.Zero, SetClipboardData((uint)StandardClipboardFormats.Bitmap, hBitmap));
            }

            var snapshot = await ClipboardNative.ReadSnapshotAsync();
            Assert.Contains("CF_BITMAP", snapshot.SkippedFormats);
            Assert.DoesNotContain("CF_BITMAP", snapshot.Formats);
            Assert.Contains("CF_DIB", snapshot.Formats);
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    [WpfFact]
    public async Task Snapshot_ToContents_RestoresTheClipboard()
    {
        await WriteTestContent();
        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_UNICODETEXT", JsonFormat });

        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Temporary")));
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(snapshot.ToContents()));

        var restored = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_UNICODETEXT", JsonFormat });
        Assert.Equal("Snapshot text ✓", restored.GetAsUnicodeString());
        Assert.Equal(snapshot.GetAsBytes(JsonFormat), restored.GetAsBytes(JsonFormat));
    }

    // ── B3: one consumer for every IClipboardDataSource ──────────────────────

    /// <summary>
    /// Consumer code which doesn't know where the data comes from
    /// </summary>
    private static (string Text, string Json, IReadOnlyList<string> Files, bool HasJson) Consume(IClipboardDataSource source)
    {
        source.TryGetAsUtf8String(JsonFormat, out var json);
        return (source.GetAsUnicodeString(), json, source.GetFileNames(), source.HasFormat(JsonFormat));
    }

    [WpfFact]
    public async Task DataSource_SameResultsForTheOpenClipboardAndASnapshot()
    {
        var longPath = @"C:\" + string.Join(@"\", Enumerable.Repeat("a-rather-long-folder-name", 14)) + @"\file.txt";
        Assert.True(longPath.Length > 300);
        var files = new[] { @"C:\Temp\ä ö ü.txt", longPath };
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents()
            .AddUnicodeString("Data source text")
            .AddBytes(Encoding.UTF8.GetBytes("{\"b\":2}"), JsonFormat)
            .AddFileNames(files)));

        var fromClipboard = await ClipboardNative.UseAsync(clipboard =>
        {
            var source = clipboard.AsDataSource();
            Assert.Contains("CF_HDROP", source.Formats);
            Assert.True(source.HasFormat(StandardClipboardFormats.UnicodeText));
            return Consume(source);
        });
        var fromSnapshot = Consume(await ClipboardNative.ReadSnapshotAsync());

        Assert.Equal("Data source text", fromClipboard.Text);
        Assert.Equal("{\"b\":2}", fromClipboard.Json);
        Assert.Equal(files, fromClipboard.Files);
        Assert.True(fromClipboard.HasJson);

        Assert.Equal(fromClipboard.Text, fromSnapshot.Text);
        Assert.Equal(fromClipboard.Json, fromSnapshot.Json);
        Assert.Equal(fromClipboard.Files, fromSnapshot.Files);
        Assert.True(fromSnapshot.HasJson);
    }

    [WpfFact]
    public async Task DataSource_MissingFormats_ReturnNullOrEmpty()
    {
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddBytes(new byte[] { 1 }, JsonFormat)));
        var snapshot = await ClipboardNative.ReadSnapshotAsync();

        Assert.Null(snapshot.GetAsUnicodeString());
        Assert.Empty(snapshot.GetFileNames());
        Assert.False(snapshot.TryGetStream("Dapplo.Windows.Tests.NotOnTheClipboard", out _));
        Assert.Null(snapshot.GetAsBytes("Dapplo.Windows.Tests.NotOnTheClipboard"));
    }

    [WpfFact]
    public async Task DataSource_OfToken_OnOtherThread_Throws()
    {
        using var clipboard = ClipboardNative.Access();
        var source = clipboard.AsDataSource();
        var exception = await Task.Run(() => Record.Exception(() => source.HasFormat("CF_UNICODETEXT")));
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void ParseDropFiles_AnsiNames()
    {
        // DROPFILES with fWide = 0 and two ANSI names
        var names = Encoding.ASCII.GetBytes("C:\\a.txt\0C:\\b.txt\0\0");
        var bytes = new byte[20 + names.Length];
        BitConverter.GetBytes(20).CopyTo(bytes, 0);
        names.CopyTo(bytes, 20);

        var source = new FakeSource(StandardClipboardFormats.Drop.AsString(), bytes);
        Assert.Equal(new[] { "C:\\a.txt", "C:\\b.txt" }, source.GetFileNames());
    }

    private sealed class FakeSource : IClipboardDataSource
    {
        private readonly string _format;
        private readonly byte[] _bytes;

        public FakeSource(string format, byte[] bytes)
        {
            _format = format;
            _bytes = bytes;
        }

        public IReadOnlyCollection<string> Formats => new[] { _format };

        public bool HasFormat(string format) => format == _format;

        public bool TryGetStream(string format, out Stream stream)
        {
            stream = format == _format ? new MemoryStream(_bytes, false) : null;
            return stream != null;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
}
