// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// A copy of clipboard formats in memory, read in one short clipboard session with <see cref="ClipboardNative.ReadSnapshotAsync(IEnumerable{string}, ClipboardAccessOptions, System.Threading.CancellationToken)"/>
/// or <see cref="ClipboardSnapshotExtensions.ReadSnapshot"/>. Decoding, saving or uploading the data happens afterwards, while other
/// applications can use the clipboard again. The snapshot doesn't change when the clipboard changes, and can be used on any thread.
/// Request only the formats you need, reading a format makes the application which copied render it: <see cref="ClipboardNative.AvailableFormats(IEnumerable{string}, int)"/>
/// selects them without opening the clipboard.
/// </summary>
public sealed class ClipboardSnapshot : IClipboardDataSource
{
    private readonly Dictionary<string, byte[]> _data;

    internal ClipboardSnapshot(uint sequenceNumber, IntPtr ownerHandle, IList<string> formats, Dictionary<string, byte[]> data, IList<string> skippedFormats)
    {
        SequenceNumber = sequenceNumber;
        OwnerHandle = ownerHandle;
        Formats = new List<string>(formats).AsReadOnly();
        _data = data;
        SkippedFormats = new List<string>(skippedFormats).AsReadOnly();
    }

    /// <summary>
    /// The clipboard sequence number of the content, compare it with <see cref="ClipboardNative.SequenceNumber"/> or <see cref="ClipboardUpdateInformation.Id"/>
    /// to see if the clipboard changed since
    /// </summary>
    public uint SequenceNumber { get; }

    /// <summary>
    /// The window which owned the clipboard content
    /// </summary>
    public IntPtr OwnerHandle { get; }

    /// <summary>
    /// When the snapshot was taken
    /// </summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.Now;

    /// <summary>
    /// The formats in this snapshot, in the order the clipboard listed them (the order of preference of the application which copied)
    /// </summary>
    public IReadOnlyCollection<string> Formats { get; }

    /// <summary>
    /// Formats which were on the clipboard (and requested) but aren't in the snapshot: handle formats (CF_BITMAP, CF_ENHMETAFILE, CF_PALETTE, …),
    /// formats larger than the size limit, and formats which couldn't be rendered
    /// </summary>
    public IReadOnlyCollection<string> SkippedFormats { get; }

    /// <inheritdoc />
    public bool HasFormat(string format) => format != null && _data.ContainsKey(format);

    /// <summary>
    /// The size of the data of a format in bytes, -1 when the format isn't in the snapshot
    /// </summary>
    /// <param name="format">string with the format name</param>
    /// <returns>long</returns>
    public long GetSize(string format) => format != null && _data.TryGetValue(format, out var bytes) ? bytes.LongLength : -1;

    /// <summary>
    /// A read-only MemoryStream on the data of the format, no copy. The stream exposes the snapshot's own array:
    /// use <see cref="MemoryStream.TryGetBuffer"/> to read it without copying (e.g. to decode a large bitmap with
    /// <see cref="DibImage.TryDecode(ReadOnlySpan{byte}, long, Span{byte}, int)"/>), but don't change the buffer, it is the snapshot's data.
    /// </summary>
    /// <param name="format">string with the format name</param>
    /// <param name="stream">Stream, a read-only MemoryStream</param>
    /// <returns>true when the snapshot has the format</returns>
    public bool TryGetStream(string format, out Stream stream)
    {
        if (format == null || !_data.TryGetValue(format, out var bytes))
        {
            stream = null;
            return false;
        }
        // Read-only view on the data, no copy; publicly visible so TryGetBuffer works (the buffer must not be changed)
        stream = new MemoryStream(bytes, 0, bytes.Length, false, true);
        return true;
    }

    /// <summary>
    /// Use the virtual files (FileGroupDescriptorW + FileContents, e.g. Outlook attachments) of the clipboard content this snapshot was taken of.
    /// Their content can't be read with the Win32 clipboard API, it needs the OLE data object of the clipboard, and so an STA thread with OLE
    /// initialized (e.g. the UI thread). This takes the OLE data object only when the snapshot has a file group descriptor, this is an STA
    /// thread and the clipboard didn't change since the snapshot (<see cref="SequenceNumber"/>), lets <paramref name="use"/> read the files,
    /// and releases the data object again.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>Include <see cref="DataObjectReader.FileGroupDescriptorWFormat"/> and <see cref="DataObjectReader.FileGroupDescriptorFormat"/>
    /// in the formats of the snapshot, see <see cref="ClipboardDataSourceExtensions.HasVirtualFiles"/>.</item>
    /// <item>The data object is only valid inside <paramref name="use"/>: read the content there with <see cref="VirtualFile.OpenContent"/>
    /// and return materialized results. Calling OpenContent after <paramref name="use"/> returned throws an ObjectDisposedException.</item>
    /// <item><paramref name="use"/> runs synchronously on the calling thread; the clipboard isn't open meanwhile, but opening the OLE data
    /// object retries shortly (up to about 100 ms) when another application has the clipboard open.</item>
    /// <item>Exceptions of <paramref name="use"/> are passed on; failing to get the data object returns false.</item>
    /// </list>
    /// </remarks>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="use">Func which reads the virtual files, it's only called when there is at least one</param>
    /// <param name="result">the result of <paramref name="use"/>, default when false is returned</param>
    /// <param name="maxDataSize">long with the maximum size of one virtual file (or the descriptor) in bytes, larger ones return null from
    /// <see cref="VirtualFile.OpenContent"/>; default <see cref="DataObjectReader.DefaultMaxDataSize"/></param>
    /// <returns>true when <paramref name="use"/> was called; false when there are no virtual files, this isn't an STA thread, the clipboard
    /// changed since the snapshot, or the OLE data object couldn't be taken</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="use"/> is null</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="maxDataSize"/> isn't positive</exception>
    public bool TryUseVirtualFiles<T>(Func<IReadOnlyList<VirtualFile>, T> use, out T result, long maxDataSize = DataObjectReader.DefaultMaxDataSize)
    {
        if (use == null)
        {
            throw new ArgumentNullException(nameof(use));
        }
        if (maxDataSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDataSize), maxDataSize, "The maximum data size must be positive.");
        }
        result = default;
        // A sequence number of 0 means it's unknown, then it can't be verified that the OLE data object has the content of this snapshot
        if (!this.HasVirtualFiles() || Thread.CurrentThread.GetApartmentState() != ApartmentState.STA ||
            SequenceNumber == 0 || SequenceNumber != ClipboardNative.SequenceNumber)
        {
            return false;
        }

        DataObjectReader reader;
        try
        {
            reader = ClipboardNative.GetOleDataObject(2, TimeSpan.FromMilliseconds(50));
        }
        catch (Exception)
        {
            // Another application keeps the clipboard open (ClipboardAccessDeniedException), OLE isn't initialized on this thread
            // (InvalidOperationException), or OleGetClipboard failed otherwise (COMException and the other exceptions of Marshal.ThrowExceptionForHR)
            return false;
        }

        try
        {
            // The clipboard can have changed between the check and OleGetClipboard
            if (SequenceNumber != ClipboardNative.SequenceNumber)
            {
                return false;
            }
            reader.MaxDataSize = maxDataSize;
            var virtualFiles = reader.GetVirtualFiles();
            if (virtualFiles.Count == 0)
            {
                return false;
            }
            result = use(virtualFiles);
            return true;
        }
        finally
        {
            // Releases the data object: the virtual files can't be read after this
            reader.Dispose();
        }
    }

    /// <summary>
    /// Create the contents to write this snapshot back to the clipboard, e.g. to restore the clipboard after using it temporarily.
    /// </summary>
    /// <returns>ClipboardContents with all formats of the snapshot</returns>
    public ClipboardContents ToContents()
    {
        var contents = new ClipboardContents();
        foreach (var format in Formats)
        {
            contents.AddBytes(_data[format], format);
        }
        return contents;
    }
}
