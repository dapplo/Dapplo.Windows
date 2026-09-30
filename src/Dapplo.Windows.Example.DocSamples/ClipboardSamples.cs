// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Clipboard;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/clipboard-usage.md, wiki/Clipboard.md
/// </summary>
public static class ClipboardSamples
{
    public static void Monitor()
    {
        #region Monitor
        // Every subscriber first gets the current state, then one update per clipboard change.
        // The information is collected without opening the clipboard, on the SharedMessageWindow thread.
        IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info =>
        {
            Console.WriteLine($"Clipboard #{info.Id} from window {info.OwnerHandle}: {string.Join(", ", info.Formats)}");
        });

        // Stop monitoring
        subscription.Dispose();
        #endregion
    }

    public static void FilterByFormat()
    {
        #region FilterByFormat
        // Standard formats have names like "CF_UNICODETEXT", compare the IDs or use AsString()
        var textChanges = ClipboardNative.OnUpdate
            .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
            .Subscribe(info => Console.WriteLine("Text was copied"));

        var fileChanges = ClipboardNative.OnUpdate
            .Where(info => info.Formats.Contains(StandardClipboardFormats.Drop.AsString()))
            .Subscribe(info => Console.WriteLine("Files were copied"));

        // Registered formats are compared by name, e.g. "PNG" or "HTML Format"
        var imageChanges = ClipboardNative.OnUpdate
            .Where(info => info.Formats.Contains("PNG"))
            .Subscribe(info => Console.WriteLine("A PNG was copied"));
        #endregion
    }

    public static void ReadOnChange()
    {
        #region ReadOnChange
        var subscription = ClipboardNative.OnUpdate
            .Where(info => info.FormatIds.Contains((uint)StandardClipboardFormats.UnicodeText))
            // Don't open the clipboard on the SharedMessageWindow thread, and wait until the copying application is done
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(info =>
            {
                using var clipboard = ClipboardNative.Access();
                if (clipboard.CanAccess)
                {
                    Console.WriteLine($"Copied: {clipboard.GetAsUnicodeString()}");
                }
            });
        #endregion
    }

    public static void MonitorOnUi()
    {
        #region MonitorOnUi
        // Call on the UI thread: the handler then runs on the UI thread, where the clipboard can be opened and the UI updated
        var subscription = ClipboardNative.OnUpdate
            .ObserveOn(SynchronizationContext.Current)
            .Subscribe(info => UpdatePasteButton(ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText)));
        #endregion
    }

    public static void ReadText()
    {
        #region ReadText
        // Access opens the clipboard on this thread, dispose the token on the same thread
        using (var clipboard = ClipboardNative.Access())
        {
            if (!clipboard.CanAccess)
            {
                // Another application kept the clipboard open (IsOpenTimeout), or another thread of this process has it (IsLockTimeout)
                return;
            }
            Console.WriteLine($"Formats: {string.Join(", ", clipboard.AvailableFormats())}");
            if (ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText))
            {
                string text = clipboard.GetAsUnicodeString();
                Console.WriteLine(text);
            }
        }
        #endregion
    }

    public static async Task UseAsync()
    {
        #region UseAsync
        // Waits asynchronously until the clipboard can be opened, then opens it, runs the work and closes it again:
        // all on one thread, so the token can't end up on another thread. The work must not await.
        string text = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsUnicodeString());

        // Write: prepare the content first, only place it inside the work
        var contents = new ClipboardContents().AddUnicodeString("Hello, World!");
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
        #endregion
    }

    public static async Task UseAsyncOptions(CancellationToken cancellationToken)
    {
        #region UseAsyncOptions
        try
        {
            var options = new ClipboardAccessOptions
            {
                // Try to open the clipboard 20 times, 50ms apart (asynchronously), wait up to 1 second for other threads of this process
                Retries = 20,
                RetryInterval = TimeSpan.FromMilliseconds(50),
                LockTimeout = TimeSpan.FromSeconds(1)
            };
            // Read the raw data while the clipboard is open, decode it afterwards
            byte[] png = await ClipboardNative.UseAsync(clipboard => ClipboardNative.HasFormat("PNG") ? clipboard.GetAsBytes("PNG") : null, options, cancellationToken);
        }
        catch (ClipboardAccessDeniedException ex)
        {
            Console.WriteLine($"The clipboard is in use: {ex.Message}");
        }
        #endregion
    }

    public static async Task Snapshot()
    {
        #region Snapshot
        // Copy the formats you need in one short clipboard session...
        ClipboardSnapshot snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "PNG", StandardClipboardFormats.UnicodeText.AsString() });

        // ...then decode, save or upload while other applications can use the clipboard again
        if (snapshot.TryGetStream("PNG", out var pngStream))
        {
            using (pngStream)
            using (var file = File.Create(@"C:\Temp\pasted.png"))
            {
                await pngStream.CopyToAsync(file);
            }
        }
        string text = snapshot.GetAsUnicodeString();

        // Has the clipboard changed since?
        bool changed = snapshot.SequenceNumber != ClipboardNative.SequenceNumber;
        #endregion
    }

    public static async Task SelectFormats()
    {
        #region SelectFormats
        // The best two image formats which are available, checked without opening the clipboard.
        // Reading a format makes the application which copied render it, so only request what you need;
        // the second format is a fallback when the first can't be decoded.
        string[] imageFormats = { "PNG", "CF_DIBV5", "JFIF", "CF_DIB", "GIF" };
        IReadOnlyList<string> formats = ClipboardNative.AvailableFormats(imageFormats, 2);
        ClipboardSnapshot snapshot = await ClipboardNative.ReadSnapshotAsync(formats);
        #endregion
    }

    public static async Task DataSource()
    {
        #region DataSource
        // Written once, works for the open clipboard, a snapshot and other IClipboardDataSource implementations
        static string Describe(IClipboardDataSource source)
        {
            IReadOnlyList<string> files = source.GetFileNames();
            if (files.Count > 0)
            {
                return $"{files.Count} file(s)";
            }
            return source.GetAsUnicodeString() ?? $"Formats: {string.Join(", ", source.Formats)}";
        }

        string fromClipboard = await ClipboardNative.UseAsync(clipboard => Describe(clipboard.AsDataSource()));
        string fromSnapshot = Describe(await ClipboardNative.ReadSnapshotAsync());
        #endregion
    }

    public static async Task WhoBlocks()
    {
        #region WhoBlocks
        try
        {
            await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Hello")));
        }
        catch (ClipboardAccessDeniedException ex) when (ex.IsOpenTimeout)
        {
            // Tell the user which application keeps the clipboard open, e.g. "notepad.exe"
            Console.WriteLine($"The clipboard is in use by {ex.BlockingProcessName ?? "an unknown application"}");
        }

        // The same for a token which couldn't open the clipboard
        using var token = ClipboardNative.Access();
        if (token.IsOpenTimeout)
        {
            Console.WriteLine($"The clipboard is in use by {token.GetBlockingProcessName() ?? "an unknown application"}");
        }
        #endregion
    }

    public static async Task Html()
    {
        #region Html
        // Write: the header with the UTF-8 byte offsets is created for you
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents()
            .AddHtml("<p>Hello <b>World</b></p>", new Uri("https://example.com/"))
            .AddUnicodeString("Hello World")));

        // Read: what a browser or Word copied
        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { ClipboardHtml.FormatName });
        if (snapshot.TryGetAsHtml(out ClipboardHtml html))
        {
            Console.WriteLine($"Copied from {html.SourceUrl}: {html.Fragment}");
        }
        #endregion
    }

    public static async Task Dib()
    {
        #region Dib
        // Read a bitmap as top-down BGRA32 pixels (CF_DIBV5, or CF_DIB), and hand them to any imaging library
        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIBV5", "CF_DIB" });
        if (snapshot.TryGetAsDib(out DibImage image))
        {
            Console.WriteLine($"{image.Width}x{image.Height}, alpha: {image.HasAlpha}, {image.Pixels.Length} bytes");
        }

        // Write BGRA32 pixels as CF_DIBV5 (with alpha) and CF_DIB (for older applications)
        byte[] pixels = new byte[16 * 16 * 4];
        await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(new ClipboardContents()
            .AddDib(pixels, 16, 16, 16 * 4, premultipliedAlpha: false)));
        #endregion
    }

    public static async Task DibMaxSize()
    {
        #region DibMaxSize
        // Bitmaps larger than DibImage.DefaultMaxPixelCount (64 megapixels) aren't decoded, the size is checked from the header
        // before anything is allocated. Pass your own maximum (width * height), e.g. for very large scans:
        var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIBV5", "CF_DIB" });
        if (snapshot.TryGetAsDib(16384L * 16384, out DibImage image))
        {
            Console.WriteLine($"{image.Width}x{image.Height}");
        }
        #endregion
    }

    public static async Task EnhancedMetafile()
    {
        #region EnhancedMetafile
        // Vector graphics from Office or Visio, as the bytes of an .emf file
        byte[] emf = await ClipboardNative.UseAsync(clipboard => clipboard.TryGetEnhancedMetafileBits(out var bits) ? bits : null);
        if (emf != null)
        {
            File.WriteAllBytes(@"C:\Temp\copied.emf", emf);
        }
        #endregion
    }

    public static async Task DelayedRenderingFunc()
    {
        #region DelayedRenderingFunc
        await ClipboardNative.UseAsync(clipboard =>
        {
            clipboard.ClearContents();
            clipboard.SetAsUnicodeString("A large export");
            // Rendered only when an application pastes the format; the renderer is dropped when the content is replaced
            clipboard.SetDelayedRenderedContent("MyApp.Export", () => new MemoryStream(CreateLargeData()));
        });
        #endregion
    }

    public static void VirtualFiles()
    {
        #region VirtualFiles
        // On the UI thread (STA with OLE initialized), e.g. in a drop handler or for a paste
        using (DataObjectReader reader = ClipboardNative.GetOleDataObject())
        {
            foreach (VirtualFile file in reader.GetVirtualFiles())
            {
                if (file.IsDirectory)
                {
                    continue;
                }
                // Read the content now, the data object is only valid for a short time
                using Stream content = file.OpenContent();
                if (content == null)
                {
                    continue;
                }
                using var target = File.Create(Path.Combine(@"C:\Temp", file.SafeFileName));
                content.CopyTo(target);
            }

            // It's an IClipboardDataSource too: the same helpers as for the clipboard and snapshots
            string text = reader.GetAsUnicodeString();
        }

        // A data object from a drop event (System.Runtime.InteropServices.ComTypes.IDataObject): the reader doesn't release it
        // var reader = new DataObjectReader((System.Runtime.InteropServices.ComTypes.IDataObject)e.Data);
        #endregion
    }

    public static async Task VirtualFilesFromSnapshot()
    {
        #region VirtualFilesFromSnapshot
        // On the UI thread (STA with OLE initialized): the await continues there.
        // Copy what you need in one short session, including the descriptor of virtual files (e.g. Outlook attachments)
        var formats = ClipboardNative.AvailableFormats(new[] { "PNG", DataObjectReader.FileGroupDescriptorWFormat, DataObjectReader.FileGroupDescriptorFormat });
        ClipboardSnapshot snapshot = await ClipboardNative.ReadSnapshotAsync(formats);

        // Reads the virtual files through OLE, only when the clipboard didn't change since the snapshot
        if (snapshot.TryUseVirtualFiles(files =>
            {
                // The files can only be read in here: copy the content now
                var saved = new List<string>();
                foreach (VirtualFile file in files.Where(f => !f.IsDirectory))
                {
                    using Stream content = file.OpenContent();
                    if (content == null)
                    {
                        continue;
                    }
                    string path = Path.Combine(@"C:\Temp", file.SafeFileName);
                    using var target = File.Create(path);
                    content.CopyTo(target);
                    saved.Add(path);
                }
                return saved;
            }, out List<string> savedFiles, maxDataSize: 256L * 1024 * 1024))
        {
            Console.WriteLine($"Saved {savedFiles.Count} file(s)");
        }
        #endregion
    }

    public static void DropText(System.Runtime.InteropServices.ComTypes.IDataObject dataObject)
    {
        #region DropText
        // A drop only has the formats of its source, Windows doesn't synthesize CF_UNICODETEXT there.
        // With only CF_TEXT, GetAsUnicodeString decodes it with the code page of CF_LOCALE (else the ANSI code page),
        // with only CF_OEMTEXT with the OEM code page.
        using var reader = new DataObjectReader(dataObject);
        string text = reader.GetAsUnicodeString();
        #endregion
    }

    public static async Task ReadTextAsync()
    {
        #region ReadTextAsync
        // Only the waiting for the clipboard is asynchronous, the clipboard is opened on the thread which continues after the await.
        // Don't await again while holding the token.
        using var clipboard = await ClipboardNative.AccessAsync();
        string text = clipboard.CanAccess ? clipboard.GetAsUnicodeString() : null;
        #endregion
    }

    public static void ReadFiles()
    {
        #region ReadFiles
        using var clipboard = ClipboardNative.Access();
        foreach (var fileName in clipboard.GetFileNames())
        {
            Console.WriteLine(fileName);
        }
        #endregion
    }

    public static void ReadImage()
    {
        #region ReadImage
        using var clipboard = ClipboardNative.Access();
        // Most applications also place a PNG, which keeps the transparency
        if (clipboard.TryGetAsStream("PNG", out var pngStream))
        {
            // The stream is a copy, it stays valid after the token is disposed
            using (pngStream)
            using (var bitmap = new Bitmap(pngStream))
            {
                Console.WriteLine($"Image of {bitmap.Width}x{bitmap.Height}");
            }
        }
        #endregion
    }

    public static void ReadCustomFormat()
    {
        #region ReadCustomFormat
        using var clipboard = ClipboardNative.Access();
        if (clipboard.AvailableFormats().Contains("MyApp.Settings"))
        {
            byte[] data = clipboard.GetAsBytes("MyApp.Settings");
            Console.WriteLine($"{data.Length} bytes");
        }
        #endregion
    }

    public static void ReplaceContents(Bitmap bitmap)
    {
        #region ReplaceContents
        // Prepare everything before the clipboard is opened
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
        pngStream.Position = 0;

        var contents = new ClipboardContents()
            // The richest format first, the application which pastes picks the first one it understands
            .AddStream("PNG", pngStream)
            .AddUnicodeString("A screenshot")
            .AddFileNames(new[] { @"C:\Temp\screenshot.png" })
            // Optional: clipboard history (Win+V) and cloud clipboard
            .WithCloudClipboardOptions(canUploadToCloud: false);

        // Opens the clipboard, clears it, places all formats and closes it again.
        // Throws a ClipboardAccessDeniedException when the clipboard can't be opened.
        ClipboardNative.ReplaceContents(contents);
        #endregion
    }

    public static async Task ReplaceContentsAsync()
    {
        #region ReplaceContentsAsync
        var contents = new ClipboardContents().AddUnicodeString("Hello, World!");
        // Only the waiting for the clipboard is asynchronous, the clipboard is written on the thread which continues after the await
        await ClipboardNative.ReplaceContentsAsync(contents);

        // With a token you already have: ReplaceContents always clears first
        using var clipboard = await ClipboardNative.AccessAsync();
        clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Hello again"));
        #endregion
    }

    public static void AddToCurrentContents()
    {
        #region AddToCurrentContents
        using var clipboard = ClipboardNative.Access();
        // Adding to the current content is only allowed when you placed it (the window of the token owns the clipboard),
        // otherwise this throws an InvalidOperationException instead of mixing your format into the content of another application
        clipboard.AddToCurrentContents(new ClipboardContents().AddBytes(Encoding.UTF8.GetBytes("{\"id\":42}"), "MyApp.Reference"));
        #endregion
    }

    public static void WriteText()
    {
        #region WriteText
        using var clipboard = ClipboardNative.Access();
        // Always clear first: this removes the previous content and makes the window of the token the clipboard owner
        clipboard.ClearContents();
        clipboard.SetAsUnicodeString("Hello, World!");
        #endregion
    }

    public static void WriteFiles()
    {
        #region WriteFiles
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        // Explorer can paste these, the paths must be fully qualified
        clipboard.SetFileNames(new[] { @"C:\Temp\report.pdf", @"C:\Temp\image.png" });
        #endregion
    }

    public static void WriteMultipleFormats()
    {
        #region WriteMultipleFormats
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        // Place several formats of the same content, the application which pastes picks the best one
        clipboard.SetAsUnicodeString("Hello, World!");
        clipboard.SetAsBytes(Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello, {\b World}!}"), "Rich Text Format");
        // Your own format, it's registered on first use
        clipboard.SetAsBytes(Encoding.UTF8.GetBytes("{\"greeting\":\"Hello\"}"), "MyApp.Settings");
        #endregion
    }

    public static void WriteStream(Bitmap bitmap)
    {
        #region WriteStream
        using var pngStream = new MemoryStream();
        bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
        pngStream.Position = 0;

        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        clipboard.SetAsStream("PNG", pngStream);
        #endregion
    }

    public static void Clear()
    {
        #region Clear
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        #endregion
    }

    public static void DelayedRendering()
    {
        #region DelayedRendering
        // 1. Register the renderer, keep the registration as long as the content can be requested
        IDisposable registration = ClipboardNative.RegisterDelayedRenderer("MyApp.LargeData", request =>
        {
            // Called on the SharedMessageWindow thread when an application pastes the format.
            // Render right here, with the token of the request: don't open the clipboard, await or switch threads.
            byte[] data = CreateLargeData();
            request.AccessToken.SetAsBytes(data, request.RequestedFormatId);
        });

        // 2. Announce the format, the data is created only when somebody pastes it.
        // ReplaceContents clears the clipboard, this makes the SharedMessageWindow the owner which gets the render requests.
        ClipboardNative.ReplaceContents(new ClipboardContents().AddDelayedRendered("MyApp.LargeData"));

        // 3. Keep the registration until the process exits: then the SharedMessageWindow is destroyed,
        // and the renderer is called for every format which nobody requested yet (WM_RENDERALLFORMATS), so the content survives your process.
        // Disposing it earlier means these formats can't be rendered anymore.
        registration.Dispose();
        #endregion
    }

    public static void CloudOptionsSensitive()
    {
        #region CloudOptionsSensitive
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        clipboard.SetAsUnicodeString("MyPassword123!");
        // Not in the clipboard history (Win+V), not synced to other devices, ignored by clipboard monitors
        clipboard.ExcludeFromMonitorProcessing();
        #endregion
    }

    public static void CloudOptions()
    {
        #region CloudOptions
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        clipboard.SetAsUnicodeString("Temporary value");
        // Keep it out of the history and the cloud, but let clipboard managers see it.
        // Options which are not passed (null) are not placed, then the user's settings apply.
        clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: false);
        #endregion
    }

    public static void CloudOptionsSingle()
    {
        #region CloudOptionsSingle
        using var clipboard = ClipboardNative.Access();
        clipboard.ClearContents();
        clipboard.SetAsUnicodeString("Shared snippet");
        clipboard.SetCanIncludeInClipboardHistory(true);
        clipboard.SetCanUploadToCloudClipboard(false);
        #endregion
    }

    public static void AccessDenied()
    {
        #region AccessDenied
        try
        {
            // Wait up to 1 second for another thread of this process, and try to open the clipboard 10 times, 50ms apart
            using var clipboard = ClipboardNative.Access(retries: 10, retryInterval: TimeSpan.FromMilliseconds(50), timeout: TimeSpan.FromSeconds(1));
            // The Get / Set extension methods throw a ClipboardAccessDeniedException when the token has no access
            var text = clipboard.GetAsUnicodeString();
        }
        catch (ClipboardAccessDeniedException ex)
        {
            Console.WriteLine($"The clipboard is in use: {ex.Message}");
        }
        #endregion
    }

    private static void UpdatePasteButton(bool canPaste) { }
    private static byte[] CreateLargeData() => new byte[1024];
}
