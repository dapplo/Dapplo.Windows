# Clipboard

**Dapplo.Windows.Clipboard** monitors the clipboard with an observable and reads and writes its content in any
format, with support for delayed rendering and the Windows clipboard history / cloud clipboard options.

```powershell
dotnet add package Dapplo.Windows.Clipboard
```

Namespaces used on this page: `Dapplo.Windows.Clipboard`, `System.Reactive.Linq`.

## Monitoring changes

`ClipboardNative.OnUpdate` reports every clipboard change. A subscriber first gets the current state, then one
`ClipboardUpdateInformation` per change. The information (`Id`, `OwnerHandle`, `Formats`, `FormatIds`, `Timestamp`) is
collected *without* opening the clipboard, so it never blocks and never fails because another application has the
clipboard open.

<!-- sample: ClipboardSamples.Monitor -->
```csharp
// Every subscriber first gets the current state, then one update per clipboard change.
// The information is collected without opening the clipboard, on the SharedMessageWindow thread.
IDisposable subscription = ClipboardNative.OnUpdate.Subscribe(info =>
{
    Console.WriteLine($"Clipboard #{info.Id} from window {info.OwnerHandle}: {string.Join(", ", info.Formats)}");
});

// Stop monitoring
subscription.Dispose();
```

Standard formats are named like the Win32 constants (`"CF_UNICODETEXT"`, `"CF_HDROP"`), registered formats by their
registered name (`"PNG"`, `"HTML Format"`). Compare IDs for standard formats, or use `StandardClipboardFormats.X.AsString()`:

<!-- sample: ClipboardSamples.FilterByFormat -->
```csharp
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
```

### Threading

`OnUpdate` publishes on the SharedMessageWindow thread. Don't open the clipboard there and don't do slow work there:
move to another thread first. `Throttle` does that and also gives the copying application time to finish:

<!-- sample: ClipboardSamples.ReadOnChange -->
```csharp
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
```

In a UI application, `ObserveOn` the UI thread:

<!-- sample: ClipboardSamples.MonitorOnUi -->
```csharp
// Call on the UI thread: the handler then runs on the UI thread, where the clipboard can be opened and the UI updated
var subscription = ClipboardNative.OnUpdate
    .ObserveOn(SynchronizationContext.Current)
    .Subscribe(info => UpdatePasteButton(ClipboardNative.HasFormat(StandardClipboardFormats.UnicodeText)));
```

## Access: the clipboard lock

The clipboard is one resource for all applications. To read or write it, open it with `ClipboardNative.Access()`, do
your work, and dispose the returned `IClipboardAccessToken`. While you hold it, no other application can use the
clipboard, so keep it short.

- The clipboard works on any thread, no STA thread is needed.
- Windows ties the opened clipboard to the thread which opened it. Use and dispose the token on that thread: on another
  thread `CanAccess` is `false`, the `Get...` / `Set...` methods throw an `InvalidOperationException`, and so does
  `Dispose` (the clipboard can only be closed on the thread which opened it; the token stays valid, dispose it on the
  right thread).
- Never `await` while you hold the token. From async code, prefer `UseAsync` (below).
- `Access()` doesn't throw when the clipboard is busy. It retries opening it (5 times, 100 ms apart by default) and
  returns a token with `CanAccess == false` and `IsOpenTimeout` (another application has it) or `IsLockTimeout`
  (another thread of your process has it).
- The `Get...` / `Set...` extension methods throw a `ClipboardAccessDeniedException` when the token has no access.

<!-- sample: ClipboardSamples.ReadText -->
```csharp
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
```

### From async code: UseAsync

`ClipboardNative.UseAsync(work)` waits asynchronously until the clipboard can be opened, and then opens it, runs `work`
and closes it again synchronously, on one thread. The waiting runs on the context of the caller, so in a UI
application `work` runs on the UI thread. It throws a `ClipboardAccessDeniedException` when the clipboard stays busy,
and cancelling throws an `OperationCanceledException`.

- `work` must not be async: the clipboard is closed when it returns. An `async` lambda doesn't compile, and work
  which returns a `Task` throws an `InvalidOperationException`.
- Copy the data out inside `work` and decode it afterwards; prepare what you write before calling `UseAsync`.
- Don't call `Access`, `AccessAsync` or `UseAsync` inside `work`, the in-process lock isn't reentrant.

<!-- sample: ClipboardSamples.UseAsync -->
```csharp
// Waits asynchronously until the clipboard can be opened, then opens it, runs the work and closes it again:
// all on one thread, so the token can't end up on another thread. The work must not await.
string text = await ClipboardNative.UseAsync(clipboard => clipboard.GetAsUnicodeString());

// Write: prepare the content first, only place it inside the work
var contents = new ClipboardContents().AddUnicodeString("Hello, World!");
await ClipboardNative.UseAsync(clipboard => clipboard.ReplaceContents(contents));
```

`ClipboardAccessOptions` sets the owner window, the retries and the timeouts:

<!-- sample: ClipboardSamples.UseAsyncOptions -->
```csharp
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
```

`AccessAsync()` also waits asynchronously, and opens the clipboard on the thread which continues after the `await`
(the context of the caller). Don't `await` anything else while you hold the token. Cancelling throws an
`OperationCanceledException`.

<!-- sample: ClipboardSamples.ReadTextAsync -->
```csharp
// Only the waiting for the clipboard is asynchronous, the clipboard is opened on the thread which continues after the await.
// Don't await again while holding the token.
using var clipboard = await ClipboardNative.AccessAsync();
string text = clipboard.CanAccess ? clipboard.GetAsUnicodeString() : null;
```

## Snapshots: read now, process later

`ClipboardNative.ReadSnapshotAsync(formats)` copies the formats into memory in one short clipboard session. Decoding
an image, saving a file or uploading happens afterwards, while other applications can use the clipboard again. A
snapshot never changes, and can be used on any thread.

- `formats == null` copies every format which is stored in memory. Handle formats (`CF_BITMAP`, `CF_ENHMETAFILE`,
  `CF_PALETTE`, `CF_METAFILEPICT`, the display, private and GDI object formats) are skipped; Windows synthesizes
  `CF_DIB` / `CF_DIBV5` from `CF_BITMAP`, and those are copied. Reading a format makes the copying application render
  it when it uses delayed rendering, so pass the formats you need.
- `ReadSnapshotAsync(formats, maxBytesPerFormat)` skips larger formats; `SkippedFormats` lists everything which was
  requested but isn't in the snapshot.
- `SequenceNumber` tells whether the clipboard changed since; `ToContents()` writes the snapshot back, e.g. to restore
  the clipboard after using it temporarily. With an open token use `clipboard.ReadSnapshot(formats)`.
- `ClipboardNative.AvailableFormats(preferred, max)` returns the first `max` formats of `preferred` which are available,
  in that order, without opening the clipboard (formats Windows synthesizes count). Use it to request only what you need,
  see the second sample.

<!-- sample: ClipboardSamples.Snapshot -->
```csharp
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
```

<!-- sample: ClipboardSamples.SelectFormats -->
```csharp
// The best two image formats which are available, checked without opening the clipboard.
// Reading a format makes the application which copied render it, so only request what you need;
// the second format is a fallback when the first can't be decoded.
string[] imageFormats = { "PNG", "CF_DIBV5", "JFIF", "CF_DIB", "GIF" };
IReadOnlyList<string> formats = ClipboardNative.AvailableFormats(imageFormats, 2);
ClipboardSnapshot snapshot = await ClipboardNative.ReadSnapshotAsync(formats);
```

### One reader for every source: IClipboardDataSource

`IClipboardDataSource` (`Formats`, `HasFormat`, `TryGetStream`) is implemented by `ClipboardSnapshot` and by
`clipboard.AsDataSource()` for an open clipboard. The extension methods `GetAsUnicodeString`, `TryGetAsUtf8String`,
`GetAsBytes` / `TryGetAsBytes` and `GetFileNames` work on every source; missing formats return `null` or an empty list.

<!-- sample: ClipboardSamples.DataSource -->
```csharp
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
```

## Reading

| Content | Method |
|---|---|
| Text | `GetAsUnicodeString()` (CF_UNICODETEXT), or `GetAsUnicodeString(format)` for other text formats. Sources other than the clipboard (e.g. a drop) fall back to CF_TEXT / CF_OEMTEXT |
| Files | `GetFileNames()` (CF_HDROP) |
| Any format as bytes | `GetAsBytes(format)` |
| Any format as stream | `GetAsStream(format)` / `TryGetAsStream(format, out stream)`; the stream is a copy and stays valid after the token is disposed |
| Which formats | `AvailableFormats()`, `AvailableFormatIds()`, or without opening the clipboard `ClipboardNative.HasFormat(format)` and `ClipboardNative.AvailableFormats(preferred, max)` |

<!-- sample: ClipboardSamples.ReadFiles -->
```csharp
using var clipboard = ClipboardNative.Access();
foreach (var fileName in clipboard.GetFileNames())
{
    Console.WriteLine(fileName);
}
```

<!-- sample: ClipboardSamples.ReadImage -->
```csharp
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
```

<!-- sample: ClipboardSamples.ReadCustomFormat -->
```csharp
using var clipboard = ClipboardNative.Access();
if (clipboard.AvailableFormats().Contains("MyApp.Settings"))
{
    byte[] data = clipboard.GetAsBytes("MyApp.Settings");
    Console.WriteLine($"{data.Length} bytes");
}
```

## Writing

Describe the complete content with a `ClipboardContents` and place it with `ClipboardNative.ReplaceContents`. That opens
the clipboard, clears it (the window of the token, by default the SharedMessageWindow, becomes the owner), places all
formats in one go and closes the clipboard again. Prepare the data before, so the clipboard is only open for a moment.
If a format can't be placed, the clipboard is cleared again and the exception is rethrown: other applications never see
half of your content. When the clipboard can't be opened, `ReplaceContents` throws a `ClipboardAccessDeniedException`.

<!-- sample: ClipboardSamples.ReplaceContents -->
```csharp
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
```

| `ClipboardContents` method | Format |
|---|---|
| `AddUnicodeString(text)` / `AddUnicodeString(text, format)` | CF_UNICODETEXT, or the given format |
| `AddBytes(bytes, format)` | any format |
| `AddStream(format, stream, size)` | any format; the stream is read when the content is placed, keep it open until then |
| `AddFileNames(fileNames)` | CF_HDROP |
| `AddDelayedRendered(format)` | only announced, see [Delayed rendering](#delayed-rendering) |
| `WithCloudClipboardOptions(...)`, `ExcludeFromMonitorProcessing()` | placed after the formats, see [Clipboard history and cloud clipboard](#clipboard-history-and-cloud-clipboard) |

The formats are placed in the order they were added, put the richest first. Adding a format twice throws an
`ArgumentException`. With a token you already hold, `token.ReplaceContents(contents)` does the same; it always clears
first:

<!-- sample: ClipboardSamples.ReplaceContentsAsync -->
```csharp
var contents = new ClipboardContents().AddUnicodeString("Hello, World!");
// Only the waiting for the clipboard is asynchronous, the clipboard is written on the thread which continues after the await
await ClipboardNative.ReplaceContentsAsync(contents);

// With a token you already have: ReplaceContents always clears first
using var clipboard = await ClipboardNative.AccessAsync();
clipboard.ReplaceContents(new ClipboardContents().AddUnicodeString("Hello again"));
```

Adding formats to the current content, without clearing it, is only possible with the explicitly named
`AddToCurrentContents`, and only while your window owns the content:

<!-- sample: ClipboardSamples.AddToCurrentContents -->
```csharp
using var clipboard = ClipboardNative.Access();
// Adding to the current content is only allowed when you placed it (the window of the token owns the clipboard),
// otherwise this throws an InvalidOperationException instead of mixing your format into the content of another application
clipboard.AddToCurrentContents(new ClipboardContents().AddBytes(Encoding.UTF8.GetBytes("{\"id\":42}"), "MyApp.Reference"));
```

### Low level: ClearContents and Set...

The `Set...` extension methods place one format each. Call `ClearContents()` first: it removes the previous content
and makes the window of the token the clipboard owner. When the content belongs to another window, every `Set...`
method throws an `InvalidOperationException` instead of silently adding your format to the content of another
application. The check is reliable: while you hold the clipboard open, nobody else can empty it, so the owner can't
change. Adding more formats to content you placed yourself (for example with the same token) is fine.

<!-- sample: ClipboardSamples.WriteText -->
```csharp
using var clipboard = ClipboardNative.Access();
// Always clear first: this removes the previous content and makes the window of the token the clipboard owner
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Hello, World!");
```

<!-- sample: ClipboardSamples.WriteFiles -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
// Explorer can paste these, the paths must be fully qualified
clipboard.SetFileNames(new[] { @"C:\Temp\report.pdf", @"C:\Temp\image.png" });
```

<!-- sample: ClipboardSamples.WriteStream -->
```csharp
using var pngStream = new MemoryStream();
bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
pngStream.Position = 0;

using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsStream("PNG", pngStream);
```

Offer the same content in several formats, the application which pastes picks the best one it understands:

<!-- sample: ClipboardSamples.WriteMultipleFormats -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
// Place several formats of the same content, the application which pastes picks the best one
clipboard.SetAsUnicodeString("Hello, World!");
clipboard.SetAsBytes(Encoding.ASCII.GetBytes(@"{\rtf1\ansi Hello, {\b World}!}"), "Rich Text Format");
// Your own format, it's registered on first use
clipboard.SetAsBytes(Encoding.UTF8.GetBytes("{\"greeting\":\"Hello\"}"), "MyApp.Settings");
```

"HTML Format" is not plain HTML: it needs a header with byte offsets, see
[HTML Clipboard Format](https://learn.microsoft.com/en-us/windows/win32/dataxchg/html-clipboard-format).

To empty the clipboard:

<!-- sample: ClipboardSamples.Clear -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
```

## HTML, bitmaps and metafiles

These helpers work on raw bytes, without System.Drawing, WinForms or WPF. The readers take any `IClipboardDataSource`
(a snapshot, `clipboard.AsDataSource()`, …).

**CF_HTML** (`"HTML Format"`): `AddHtml` / `SetAsHtml` write a fragment with a correct header (UTF-8 byte offsets, optional
`SourceURL`). `TryGetAsHtml` returns the `Fragment`, the `FullHtml` context and the `SourceUrl`; it uses the header offsets
when they are consistent and falls back to the `<!--StartFragment-->` / `<!--EndFragment-->` comments when a producer
counted characters instead of bytes. `ClipboardHtml.Create` / `TryParse` do the same without the clipboard.

<!-- sample: ClipboardSamples.Html -->
```csharp
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
```

**CF_DIB / CF_DIBV5**: `TryGetAsDib` returns a `DibImage` with top-down BGRA32 pixels and straight alpha. It reads
BITMAPINFOHEADER, V4 and V5 headers, BI_RGB (1, 4, 8 bpp with palette, 16, 24, 32 bpp) and BI_BITFIELDS (16, 32 bpp),
bottom-up and top-down, masks which some writers repeat after a V5 header (Greenshot even byte-reversed), and the old
BITMAPCOREHEADER.
32 bpp BI_RGB has an alpha channel only when some pixel has a non-zero fourth byte. Windows synthesizes CF_DIB and
CF_DIBV5 from CF_BITMAP, so this reads GDI bitmaps too. `AddDib` / `SetAsDib` write CF_DIBV5 (32 bpp BI_BITFIELDS, sRGB,
straight alpha; premultiplied input is converted) and CF_DIB (32 bpp BI_RGB, many applications ignore its alpha).
Also place a `"PNG"` format when you can.

<!-- sample: ClipboardSamples.Dib -->
```csharp
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
```

Crafted or broken headers can claim huge bitmaps. The decoder checks the size from the header before it allocates
anything, overflow-safe: bitmaps with more than `DibImage.DefaultMaxPixelCount` pixels (64 megapixels, 256 MiB of
BGRA32 pixels) return `false`. `TryDecode(dib, maxPixelCount, out image)` and `TryGetAsDib(maxPixelCount, out image)`
take another maximum (width * |height|).

<!-- sample: ClipboardSamples.DibMaxSize -->
```csharp
// Bitmaps larger than DibImage.DefaultMaxPixelCount (64 megapixels) aren't decoded, the size is checked from the header
// before anything is allocated. Pass your own maximum (width * height), e.g. for very large scans:
var snapshot = await ClipboardNative.ReadSnapshotAsync(new[] { "CF_DIBV5", "CF_DIB" });
if (snapshot.TryGetAsDib(16384L * 16384, out DibImage image))
{
    Console.WriteLine($"{image.Width}x{image.Height}");
}
```

**CF_ENHMETAFILE**: `TryGetEnhancedMetafileBits` returns the bytes of an EMF file (`GetEnhMetaFileBits`).

<!-- sample: ClipboardSamples.EnhancedMetafile -->
```csharp
// Vector graphics from Office or Visio, as the bytes of an .emf file
byte[] emf = await ClipboardNative.UseAsync(clipboard => clipboard.TryGetEnhancedMetafileBits(out var bits) ? bits : null);
if (emf != null)
{
    File.WriteAllBytes(@"C:\Temp\copied.emf", emf);
}
```

## OLE data objects: drag and drop and virtual files

Some data only exists in an OLE data object (`System.Runtime.InteropServices.ComTypes.IDataObject`): formats with an
index (`lindex`), data in an `IStream`, and virtual files (`FileGroupDescriptorW` + `FileContents`), e.g. Outlook
attachments or images dragged from some browsers. `DataObjectReader` reads them:

- `new DataObjectReader(dataObject)` for a drop; `ClipboardNative.GetOleDataObject()` for the clipboard (OleGetClipboard,
  retries while another application has the clipboard open).
- `GetVirtualFiles()` returns name (can contain a relative path), size, attributes and times; `OpenContent()` copies the
  content (HGLOBAL or IStream) into a stream. `TryGetStream(format, index, out stream)` reads any format with an index.
- It is an `IClipboardDataSource`, so `GetAsUnicodeString`, `GetFileNames`, `TryGetAsHtml`, `TryGetAsDib`, … work on it.
  Windows synthesizes CF_UNICODETEXT only on the clipboard: when a drop only has CF_TEXT, `GetAsUnicodeString()`
  decodes it with the code page of CF_LOCALE (else the ANSI code page), or CF_OEMTEXT with the OEM code page, up to the
  first NUL. The open clipboard, snapshots and `GetOleDataObject()` keep returning only what the clipboard has.
- `HasVirtualFiles()` checks any `IClipboardDataSource`, `ClipboardNative.HasVirtualFiles()` the clipboard without
  opening it (`FileGroupDescriptorW` or `FileGroupDescriptor`).
- The data comes from another application, treat it as untrusted: file names can contain `..\` or absolute paths, so
  create files with `VirtualFile.SafeFileName`, never `Name`. Data larger than `MaxDataSize` (default 512 MiB) isn't
  read, and a failing or misbehaving source makes the `Try...` methods return `false` instead of throwing.
- **OLE needs an STA thread with OLE initialized** (every WinForms / WPF UI thread), unlike the rest of this library;
  `GetOleDataObject` throws an `InvalidOperationException` elsewhere. Read what you need right away, and dispose the
  reader. `TYMED_ISTORAGE` (e.g. an Outlook message attached to a message) isn't supported.

<!-- sample: ClipboardSamples.VirtualFiles -->
```csharp
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
```

**Virtual files from a snapshot.** A `ClipboardSnapshot` has the descriptor but not the content of virtual files.
`snapshot.TryUseVirtualFiles(use, out result, maxDataSize)` takes the OLE data object of the clipboard, lets `use` read
the files and releases it again. It returns `false` without calling `use` when the snapshot has no descriptor, the
thread isn't STA, the clipboard changed since the snapshot (`SequenceNumber`), or the data object can't be taken.
The files can only be read inside `use` (`OpenContent()` throws an `ObjectDisposedException` afterwards), so return
what you copied. `maxDataSize` (default `DataObjectReader.DefaultMaxDataSize`, 512 MiB) limits every file.

<!-- sample: ClipboardSamples.VirtualFilesFromSnapshot -->
```csharp
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
```

<!-- sample: ClipboardSamples.DropText -->
```csharp
// A drop only has the formats of its source, Windows doesn't synthesize CF_UNICODETEXT there.
// With only CF_TEXT, GetAsUnicodeString decodes it with the code page of CF_LOCALE (else the ANSI code page),
// with only CF_OEMTEXT with the OEM code page.
using var reader = new DataObjectReader(dataObject);
string text = reader.GetAsUnicodeString();
```

## Delayed rendering

With delayed rendering you announce a format and create the data only when an application pastes it. Register a
renderer for the format first, then announce the format with `ClipboardContents.AddDelayedRendered` (or, low level,
`SetDelayedRenderedContent` after `ClearContents`).

<!-- sample: ClipboardSamples.DelayedRendering -->
```csharp
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
```

- The renderer is called synchronously on the SharedMessageWindow thread when a format is requested (WM_RENDERFORMAT),
  and for all formats when the owner window is destroyed (WM_RENDERALLFORMATS, `request.RenderAllFormats` is `true`).
- When the process exits (`AppDomain.ProcessExit`), the SharedMessageWindow is destroyed on its own thread with
  `SharedMessageWindow.Shutdown`. Windows then sends WM_RENDERALLFORMATS, and the renderers run for every format which
  nobody requested yet, so the content survives your application. Keep the renderer registered until then.
- ProcessExit has a limited time budget (on .NET Framework about 2 seconds for all handlers together). The automatic
  shutdown waits at most `SharedMessageWindow.ProcessExitShutdownTimeout` (1.5 seconds). If rendering can take longer,
  call `SharedMessageWindow.Shutdown(timeout)` yourself at the end of `Main`, see
  [Shutdown](window-messages.md#shutdown). A process which is killed, or ends without ProcessExit
  (`Environment.FailFast`, an unhandled exception on .NET Framework), loses the formats which were not rendered.
- WM_DESTROYCLIPBOARD (another application cleared the clipboard) and the destruction of the window end the delayed
  rendering: nothing is requested anymore for the old content.
- Use `request.AccessToken`; it's only valid while the renderer runs. Don't call `Access()`, don't `await`, don't
  switch threads.
- Only one renderer per format can be registered at a time. `SetDelayedRenderedContent` throws an
  `InvalidOperationException` when no renderer is registered or when the token's window doesn't own the clipboard
  (call `ClearContents()` first).
- Exceptions in a renderer are written to `System.Diagnostics.Trace`.

### Delayed rendering for the current content

`SetDelayedRenderedContent(format, () => stream)` announces a format and renders it with the function when an application
pastes it. The renderer belongs to the current content: it's dropped when the content is replaced (`WM_DESTROYCLIPBOARD`),
and pending formats are still rendered at process exit (`WM_RENDERALLFORMATS`). Call `ClearContents` first, with the
default owner (the SharedMessageWindow).

- The renderer runs on the SharedMessageWindow thread while the requesting application waits in `GetClipboardData`;
  Windows only waits a limited time for the data. Render quickly; don't await or open the clipboard.
- Clipboard history (Win+V), cloud clipboard and clipboard managers usually request the formats right after the copy,
  so the renderer often runs immediately. Delayed rendering only saves work when nobody listens.

<!-- sample: ClipboardSamples.DelayedRenderingFunc -->
```csharp
await ClipboardNative.UseAsync(clipboard =>
{
    clipboard.ClearContents();
    clipboard.SetAsUnicodeString("A large export");
    // Rendered only when an application pastes the format; the renderer is dropped when the content is replaced
    clipboard.SetDelayedRenderedContent("MyApp.Export", () => new MemoryStream(CreateLargeData()));
});
```

## Clipboard history and cloud clipboard

Windows 10 1809 and later keep a clipboard history (Win+V) and can sync the clipboard to other devices. Applications
control this per content with special formats:

| Format | Effect |
|---|---|
| `ExcludeClipboardContentFromMonitorProcessing` | not in the history, not synced, and clipboard monitors should ignore it |
| `CanIncludeInClipboardHistory` (DWORD 0 / 1) | 0 keeps it out of the history, 1 allows it |
| `CanUploadToCloudClipboard` (DWORD 0 / 1) | 0 keeps it from being synced, 1 allows it |

With `ClipboardContents` use `WithCloudClipboardOptions(...)` or `ExcludeFromMonitorProcessing()`, they are placed after
the formats. Low level, place them after the content, with the same token. For passwords and other secrets use
`ExcludeFromMonitorProcessing()`:

<!-- sample: ClipboardSamples.CloudOptionsSensitive -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("MyPassword123!");
// Not in the clipboard history (Win+V), not synced to other devices, ignored by clipboard monitors
clipboard.ExcludeFromMonitorProcessing();
```

`SetCloudClipboardOptions` places only the options you pass; without arguments it places nothing and the user's
settings apply:

<!-- sample: ClipboardSamples.CloudOptions -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Temporary value");
// Keep it out of the history and the cloud, but let clipboard managers see it.
// Options which are not passed (null) are not placed, then the user's settings apply.
clipboard.SetCloudClipboardOptions(canIncludeInHistory: false, canUploadToCloud: false);
```

<!-- sample: ClipboardSamples.CloudOptionsSingle -->
```csharp
using var clipboard = ClipboardNative.Access();
clipboard.ClearContents();
clipboard.SetAsUnicodeString("Shared snippet");
clipboard.SetCanIncludeInClipboardHistory(true);
clipboard.SetCanUploadToCloudClipboard(false);
```

If you write a clipboard monitor yourself, respect `ExcludeClipboardContentFromMonitorProcessing`
(the constant is `ClipboardCloudExtensions.ExcludeClipboardContentFromMonitorProcessingFormat`).

## Errors

<!-- sample: ClipboardSamples.AccessDenied -->
```csharp
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
```

### Who blocks the clipboard?

When the clipboard can't be opened, `ClipboardAccessDeniedException` and the token (`IsOpenTimeout`) tell which window
kept it open: `BlockingWindow` and `BlockingProcessId` (zero when unknown, e.g. when the clipboard was opened without a
window). `ClipboardNative.OpenClipboardWindow` returns that window at any time. `IsLockTimeout` means another thread of
your own process holds the in-process lock.

To show the user who blocks the clipboard, use `ex.BlockingProcessName` or `token.GetBlockingProcessName()`: the file
name of the executable (e.g. `notepad.exe`, also for elevated processes), else the process name, else the window title;
`null` when unknown. It's determined once, when opening failed, together with the exception message. For your own
`IClipboardAccessToken` implementations the extension method determines it from `BlockingProcessId` / `BlockingWindow`.

<!-- sample: ClipboardSamples.WhoBlocks -->
```csharp
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
```


## Formats

| `StandardClipboardFormats` | Name | Content |
|---|---|---|
| `Text` | `CF_TEXT` | ANSI text |
| `UnicodeText` | `CF_UNICODETEXT` | UTF-16 text |
| `Bitmap` | `CF_BITMAP` | GDI bitmap handle |
| `DeviceIndependentBitmap` | `CF_DIB` | BITMAPINFO followed by the pixels |
| `DeviceIndependentBitmapV5` | `CF_DIBV5` | BITMAPV5HEADER followed by the pixels |
| `Drop` | `CF_HDROP` | list of files |
| `Locale` | `CF_LOCALE` | the locale of CF_TEXT |
| registered | `PNG` | PNG file |
| registered | `HTML Format` | HTML with a header |
| registered | `Rich Text Format` | RTF |

## See also

- [Window messages and the SharedMessageWindow](window-messages.md)
- [Common scenarios](common-scenarios.md): clipboard history, saving copied images, inserting text with a hotkey
