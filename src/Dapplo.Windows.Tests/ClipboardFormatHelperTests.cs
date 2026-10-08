// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.Messages;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
/// CF_HTML, DIB, EMF and managed delayed rendering on the real clipboard
/// </summary>
/// <remarks>Interactive: these tests change the clipboard. They are excluded by default, run them with --filter Category=Interactive.</remarks>
[Trait("Category", "Interactive")]
public class ClipboardFormatHelperTests
{
    // ── CF_HTML ──────────────────────────────────────────────────────────────

    [WpfFact]
    public async Task Html_ClipboardRoundTrip_NonAscii()
    {
        const string fragment = "<p>Grüße – ✓ 日本語</p>";
        await ClipboardNative.UseAsync(clipboard =>
        {
            clipboard.ClearContents();
            clipboard.SetAsHtml(fragment, new Uri("https://example.com/"));
            clipboard.SetAsUnicodeString("Grüße – ✓ 日本語");
        });

        var fromToken = await ClipboardNative.UseAsync(clipboard => clipboard.AsDataSource().TryGetAsHtml(out var html) ? html : null);
        Assert.NotNull(fromToken);
        Assert.Equal(fragment, fromToken.Fragment);
        Assert.Equal(new Uri("https://example.com/"), fromToken.SourceUrl);

        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { ClipboardHtml.FormatName });
        Assert.True(snapshot.TryGetAsHtml(out var fromSnapshot));
        Assert.Equal(fragment, fromSnapshot.Fragment);
    }

    [WpfFact]
    public async Task Html_AddHtml_ToContents()
    {
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddHtml("<i>Contents</i>").AddUnicodeString("Contents")));
        var snapshot = await ClipboardNative.ReadSnapshotAsync();
        Assert.True(snapshot.TryGetAsHtml(out var html));
        Assert.Equal("<i>Contents</i>", html.Fragment);
    }

    // ── DIB ──────────────────────────────────────────────────────────────────

    private static byte[] CreatePixels(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var i = (y * width + x) * 4;
                pixels[i] = (byte)(x * 20);
                pixels[i + 1] = (byte)(y * 30);
                pixels[i + 2] = 200;
                pixels[i + 3] = (byte)(x == 0 ? 0 : 128 + y);
            }
        }
        return pixels;
    }

    [WpfFact]
    public async Task Dib_ClipboardRoundTrip_WithAlpha()
    {
        const int width = 7, height = 5;
        var pixels = CreatePixels(width, height);
        await ClipboardNative.UseAsync(clipboard =>
        {
            clipboard.ClearContents();
            clipboard.SetAsDib(pixels, width, height, width * 4, false);
        });

        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIBV5", "CF_DIB" });
        Assert.True(snapshot.TryGetAsDib(out var image));
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
        Assert.True(image.HasAlpha);
        Assert.Equal(pixels, image.Pixels);

        Assert.True(DibImage.TryDecode(snapshot.GetAsBytes("CF_DIB"), out var dib));
        Assert.Equal(pixels, dib.Pixels);
    }

    /// <summary>
    /// The DIBs encoded directly into the clipboard memory (AddDib with ReadOnlyMemory, SetAsDib) are byte-identical to CreateDib / CreateDibV5,
    /// for straight and premultiplied alpha and a stride larger than width * 4
    /// </summary>
    [WpfFact]
    public async Task Dib_DirectEncode_ByteIdenticalOnTheClipboard()
    {
        const int width = 7, height = 5;
        foreach (var premultipliedAlpha in new[] { false, true })
        {
            foreach (var stride in new[] { width * 4, width * 4 + 12 })
            {
                var pixels = DibEncodeTests.CreatePixels(stride, premultipliedAlpha);
                var expectedV5 = DibImage.CreateDibV5(pixels, width, height, stride, premultipliedAlpha);
                var expectedDib = DibImage.CreateDib(pixels, width, height, stride, premultipliedAlpha);
                var label = $"premultiplied {premultipliedAlpha}, stride {stride}";

                // Deferred: encoded when the contents are placed
                var contents = new ClipboardContents().AddDib(new ReadOnlyMemory<byte>(pixels), width, height, stride, premultipliedAlpha);
                await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
                await AssertDibsAsync(expectedV5, expectedDib, label + ", AddDib");

                // Directly
                await ClipboardNative.UseAsync(clipboard =>
                {
                    clipboard.ClearContents();
                    clipboard.SetAsDib(pixels, width, height, stride, premultipliedAlpha);
                });
                await AssertDibsAsync(expectedV5, expectedDib, label + ", SetAsDib");
            }
        }
    }

    private static async Task AssertDibsAsync(byte[] expectedV5, byte[] expectedDib, string label)
    {
        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIBV5", "CF_DIB" });
        foreach (var (format, expected) in new[] { ("CF_DIBV5", expectedV5), ("CF_DIB", expectedDib) })
        {
            var actual = snapshot.GetAsBytes(format);
            Assert.NotNull(actual);
            // The clipboard memory can be larger than what was written (GlobalSize)
            Assert.True(actual.Length >= expected.Length, $"{format}, {label}: {actual.Length} bytes");
            Assert.True(expected.SequenceEqual(actual.Take(expected.Length)), $"{format}, {label}: the bytes differ");
        }
    }

    [WpfFact]
    public async Task Dib_FromCfBitmap_WindowsSynthesizedFormatsDecode()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams());
        try
        {
            using var bitmap = new Bitmap(5, 3, PixelFormat.Format24bppRgb);
            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(x * 50, y * 100, 77));
                }
            }
            var hBitmap = bitmap.GetHbitmap();
            using (var clipboard = ClipboardNative.Access(window.Handle))
            {
                clipboard.ClearContents();
                Assert.NotEqual(IntPtr.Zero, SetClipboardData((uint)StandardClipboardFormats.Bitmap, hBitmap));
            }

            // Real Windows output: CF_DIB and CF_DIBV5 synthesized from the GDI bitmap
            var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIB", "CF_DIBV5" });
            foreach (var format in new[] { "CF_DIB", "CF_DIBV5" })
            {
                Assert.True(DibImage.TryDecode(snapshot.GetAsBytes(format), out var image), format);
                Assert.Equal(5, image.Width);
                Assert.Equal(3, image.Height);
                for (var y = 0; y < 3; y++)
                {
                    for (var x = 0; x < 5; x++)
                    {
                        var i = (y * 5 + x) * 4;
                        Assert.Equal((byte)77, image.Pixels[i]);
                        Assert.Equal((byte)(y * 100), image.Pixels[i + 1]);
                        Assert.Equal((byte)(x * 50), image.Pixels[i + 2]);
                        Assert.Equal((byte)255, image.Pixels[i + 3]);
                    }
                }
            }
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    // ── EMF ──────────────────────────────────────────────────────────────────

    [WpfFact]
    public void EnhancedMetafile_IsReadAsEmfBytes()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams());
        try
        {
            var hdc = CreateEnhMetaFile(IntPtr.Zero, null, IntPtr.Zero, null);
            Assert.NotEqual(IntPtr.Zero, hdc);
            Rectangle(hdc, 10, 10, 100, 50);
            var hEmf = CloseEnhMetaFile(hdc);
            Assert.NotEqual(IntPtr.Zero, hEmf);

            using (var clipboard = ClipboardNative.Access(window.Handle))
            {
                clipboard.ClearContents();
                // The clipboard takes ownership of the metafile
                Assert.NotEqual(IntPtr.Zero, SetClipboardData((uint)StandardClipboardFormats.EnhancedMetafile, hEmf));
            }

            using (var clipboard = ClipboardNative.Access())
            {
                Assert.True(clipboard.TryGetEnhancedMetafileBits(out var emf));
                // EMR_HEADER record with the " EMF" signature at offset 40
                Assert.Equal(1, BitConverter.ToInt32(emf, 0));
                Assert.Equal(0x464D4520, BitConverter.ToInt32(emf, 40));
                Assert.Equal(emf.Length, BitConverter.ToInt32(emf, 48));
            }

            using (var clipboard = ClipboardNative.Access())
            {
                clipboard.ClearContents();
                clipboard.SetAsUnicodeString("No metafile");
            }
            using (var clipboard = ClipboardNative.Access())
            {
                Assert.False(clipboard.TryGetEnhancedMetafileBits(out var none));
                Assert.Null(none);
            }
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    // ── Managed delayed rendering ─────────────────────────────────────────────

    [WpfFact]
    public async Task DelayedRendering_Func_RenderedWhenAnotherThreadRequestsIt_DroppedWithTheContent()
    {
        const string format = "Dapplo.Windows.Tests.ClipboardFormatHelperTests.Delayed";
        var renderCount = 0;
        Stream Render()
        {
            Interlocked.Increment(ref renderCount);
            return new MemoryStream(Encoding.UTF8.GetBytes("Rendered on request"));
        }

        await ClipboardNative.UseAsync(clipboard =>
        {
            clipboard.ClearContents();
            clipboard.SetDelayedRenderedContent(format, Render);
        });
        Assert.True(ClipboardNative.HasFormat(format));

        // Another thread requests the format: WM_RENDERFORMAT goes to the SharedMessageWindow
        var data = await Task.Run(() => ClipboardNative.UseAsync(clipboard => clipboard.GetAsBytes(format)));
        Assert.Equal("Rendered on request", Encoding.UTF8.GetString(data, 0, "Rendered on request".Length));
        // A clipboard monitor (e.g. the clipboard history) may have requested it before us, the format is rendered once either way
        Assert.Equal(1, Volatile.Read(ref renderCount));

        // Replacing the content drops the renderer (WM_DESTROYCLIPBOARD)
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Other content")));
        Assert.Throws<InvalidOperationException>(() =>
        {
            using var clipboard = ClipboardNative.Access();
            clipboard.ClearContents();
            // No renderer anymore for this format: the old SetDelayedRenderedContent(format) requires a registered renderer
            clipboard.SetDelayedRenderedContent(format);
        });
    }

    [WpfFact]
    public async Task DelayedRendering_Func_RenderedWhenTheWindowShutsDown()
    {
        const string format = "Dapplo.Windows.Tests.ClipboardFormatHelperTests.RenderAll";
        await ClipboardNative.UseAsync(clipboard =>
        {
            clipboard.ClearContents();
            clipboard.SetDelayedRenderedContent(format, () => new MemoryStream(new byte[] { 1, 2, 3 }));
        });

        // WM_RENDERALLFORMATS: every pending format is rendered before the owner window is destroyed
        Assert.True(SharedMessageWindow.Shutdown(TimeSpan.FromSeconds(5)));

        var data = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsBytes(format));
        Assert.Equal(new byte[] { 1, 2, 3 }, new[] { data[0], data[1], data[2] });
    }

    [WpfFact]
    public void DelayedRendering_Func_OtherOwnerWindow_Throws()
    {
        var window = new NativeWindow();
        window.CreateHandle(new CreateParams());
        try
        {
            using var clipboard = ClipboardNative.Access(window.Handle);
            clipboard.ClearContents();
            Assert.Throws<InvalidOperationException>(() => clipboard.SetDelayedRenderedContent("Dapplo.Windows.Tests.ClipboardFormatHelperTests.Other", () => new MemoryStream()));
        }
        finally
        {
            window.DestroyHandle();
        }
    }

    [DllImport("user32", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint format, IntPtr memory);

    [DllImport("gdi32", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateEnhMetaFile(IntPtr hdcRef, string fileName, IntPtr rect, string description);

    [DllImport("gdi32")]
    private static extern IntPtr CloseEnhMetaFile(IntPtr hdc);

    [DllImport("gdi32")]
    private static extern bool Rectangle(IntPtr hdc, int left, int top, int right, int bottom);
}
