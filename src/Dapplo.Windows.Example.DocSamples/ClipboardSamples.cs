// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        // 2. Announce the format, the data is created only when somebody pastes it
        using (var clipboard = ClipboardNative.Access())
        {
            // ClearContents makes the SharedMessageWindow the owner, which gets the render requests
            clipboard.ClearContents();
            clipboard.SetDelayedRenderedContent("MyApp.LargeData");
        }

        // 3. Later, when the data can't be provided anymore
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
