// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.IO;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// A copy of clipboard formats in memory, read in one short clipboard session with <see cref="ClipboardNative.ReadSnapshotAsync(IEnumerable{string}, ClipboardAccessOptions, System.Threading.CancellationToken)"/>
/// or <see cref="ClipboardSnapshotExtensions.ReadSnapshot"/>. Decoding, saving or uploading the data happens afterwards, while other
/// applications can use the clipboard again. The snapshot doesn't change when the clipboard changes, and can be used on any thread.
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

    /// <inheritdoc />
    public bool TryGetStream(string format, out Stream stream)
    {
        if (format == null || !_data.TryGetValue(format, out var bytes))
        {
            stream = null;
            return false;
        }
        // Read-only view on the data, no copy
        stream = new MemoryStream(bytes, 0, bytes.Length, false, false);
        return true;
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
