// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// Describes the complete content for the clipboard: one or more formats of the same data, and optionally the clipboard history / cloud clipboard options.
/// Build it before opening the clipboard, then place it in one short operation with <see cref="ClipboardNative.ReplaceContents(ClipboardContents, IntPtr, int, TimeSpan?, TimeSpan?)"/>
/// or <see cref="ClipboardContentsExtensions.ReplaceContents(IClipboardAccessToken, ClipboardContents)"/>, which always clear the clipboard first.
/// </summary>
/// <remarks>
/// The formats are placed in the order they were added: add the richest format first, applications which enumerate the formats prefer the first they understand.
/// Byte arrays and file names are captured when they are added, streams are read when the content is placed: keep them open until then.
/// </remarks>
public sealed class ClipboardContents
{
    private readonly List<(uint FormatId, Action<IClipboardAccessToken> Place)> _formats = new();
    private bool? _canIncludeInHistory;
    private bool? _canUploadToCloud;
    private bool _excludeFromMonitoring;

    /// <summary>
    /// The IDs of the formats, in the order they are placed on the clipboard. The clipboard history / cloud clipboard option formats are not included.
    /// </summary>
    public IReadOnlyList<uint> FormatIds => _formats.Select(f => f.FormatId).ToList();

    /// <summary>
    /// Add a string, by default as CF_UNICODETEXT (Windows generates CF_TEXT and CF_OEMTEXT from it)
    /// </summary>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="formatId">uint with the clipboard format, default CF_UNICODETEXT</param>
    /// <returns>this, for chaining</returns>
    /// <exception cref="ArgumentException">When the format was already added</exception>
    public ClipboardContents AddUnicodeString(string text, uint formatId = (uint)StandardClipboardFormats.UnicodeText)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }
        return Add(formatId, token => token.SetAsUnicodeString(text, formatId));
    }

    /// <summary>
    /// Add a string in the specified format
    /// </summary>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="format">string with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddUnicodeString(string text, string format) => AddUnicodeString(text, ClipboardFormatExtensions.MapFormatToId(format));

    /// <summary>
    /// Add a string in the specified format
    /// </summary>
    /// <param name="text">string to place on the clipboard</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddUnicodeString(string text, StandardClipboardFormats format) => AddUnicodeString(text, (uint)format);

    /// <summary>
    /// Add bytes in the specified format
    /// </summary>
    /// <param name="bytes">byte array with the content, it is not copied: don't change it until the content was placed</param>
    /// <param name="formatId">uint with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    /// <exception cref="ArgumentException">When the format was already added</exception>
    public ClipboardContents AddBytes(byte[] bytes, uint formatId)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }
        return Add(formatId, token => token.SetAsBytes(bytes, formatId));
    }

    /// <summary>
    /// Add bytes in the specified format
    /// </summary>
    /// <param name="bytes">byte array with the content</param>
    /// <param name="format">string with the clipboard format, e.g. "PNG" or your own format (it is registered on first use)</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddBytes(byte[] bytes, string format) => AddBytes(bytes, ClipboardFormatExtensions.MapFormatToId(format));

    /// <summary>
    /// Add bytes in the specified format
    /// </summary>
    /// <param name="bytes">byte array with the content</param>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddBytes(byte[] bytes, StandardClipboardFormats format) => AddBytes(bytes, (uint)format);

    /// <summary>
    /// Add the content of a stream in the specified format, the stream is read when the content is placed (from its position at that moment).
    /// </summary>
    /// <param name="formatId">uint with the clipboard format</param>
    /// <param name="stream">Stream with the content, keep it open until the content was placed</param>
    /// <param name="size">optional long with the number of bytes to place, see <see cref="ClipboardStreamExtensions.SetAsStream(IClipboardAccessToken, uint, Stream, long?)"/></param>
    /// <returns>this, for chaining</returns>
    /// <exception cref="ArgumentException">When the format was already added</exception>
    public ClipboardContents AddStream(uint formatId, Stream stream, long? size = null)
    {
        if (stream == null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
        return Add(formatId, token => token.SetAsStream(formatId, stream, size));
    }

    /// <summary>
    /// Add the content of a stream in the specified format, the stream is read when the content is placed.
    /// </summary>
    /// <param name="format">string with the clipboard format, e.g. "PNG"</param>
    /// <param name="stream">Stream with the content, keep it open until the content was placed</param>
    /// <param name="size">optional long with the number of bytes to place</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddStream(string format, Stream stream, long? size = null) => AddStream(ClipboardFormatExtensions.MapFormatToId(format), stream, size);

    /// <summary>
    /// Add the content of a stream in the specified format, the stream is read when the content is placed.
    /// </summary>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    /// <param name="stream">Stream with the content, keep it open until the content was placed</param>
    /// <param name="size">optional long with the number of bytes to place</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddStream(StandardClipboardFormats format, Stream stream, long? size = null) => AddStream((uint)format, stream, size);

    /// <summary>
    /// Add a list of files (CF_HDROP), e.g. for Explorer
    /// </summary>
    /// <param name="fileNames">IEnumerable of string with the fully-qualified file names, the list is copied</param>
    /// <returns>this, for chaining</returns>
    /// <exception cref="ArgumentException">When CF_HDROP was already added</exception>
    public ClipboardContents AddFileNames(IEnumerable<string> fileNames)
    {
        if (fileNames == null)
        {
            throw new ArgumentNullException(nameof(fileNames));
        }
        var files = fileNames.ToList();
        return Add((uint)StandardClipboardFormats.Drop, token => token.SetFileNames(files));
    }

    /// <summary>
    /// Announce a format which is rendered on request (delayed rendering).
    /// A renderer must be registered with <see cref="ClipboardNative.RegisterDelayedRenderer(uint, Action{ClipboardRenderFormatRequest})"/> before the content is placed.
    /// </summary>
    /// <param name="formatId">uint with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    /// <exception cref="ArgumentException">When the format was already added</exception>
    public ClipboardContents AddDelayedRendered(uint formatId) => Add(formatId, token => token.SetDelayedRenderedContent(formatId));

    /// <summary>
    /// Announce a format which is rendered on request (delayed rendering), see <see cref="AddDelayedRendered(uint)"/>.
    /// </summary>
    /// <param name="format">string with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddDelayedRendered(string format) => AddDelayedRendered(ClipboardFormatExtensions.MapFormatToId(format));

    /// <summary>
    /// Announce a format which is rendered on request (delayed rendering), see <see cref="AddDelayedRendered(uint)"/>.
    /// </summary>
    /// <param name="format">StandardClipboardFormats with the clipboard format</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents AddDelayedRendered(StandardClipboardFormats format) => AddDelayedRendered((uint)format);

    /// <summary>
    /// Set the clipboard history and cloud clipboard options, they are placed after all formats.
    /// See <see cref="ClipboardCloudExtensions.SetCloudClipboardOptions"/> for the meaning of the values, options which are null are not placed.
    /// </summary>
    /// <param name="canIncludeInHistory">null: not placed, true / false: allow / prevent the clipboard history (Win+V)</param>
    /// <param name="canUploadToCloud">null: not placed, true / false: allow / prevent syncing to other devices</param>
    /// <param name="excludeFromMonitoring">true: exclude the content from history, cloud and clipboard monitors (e.g. for passwords)</param>
    /// <returns>this, for chaining</returns>
    public ClipboardContents WithCloudClipboardOptions(bool? canIncludeInHistory = null, bool? canUploadToCloud = null, bool excludeFromMonitoring = false)
    {
        _canIncludeInHistory = canIncludeInHistory;
        _canUploadToCloud = canUploadToCloud;
        _excludeFromMonitoring = excludeFromMonitoring;
        return this;
    }

    /// <summary>
    /// Exclude the content from the clipboard history, the cloud clipboard and clipboard monitors, use this for passwords and other secrets.
    /// </summary>
    /// <returns>this, for chaining</returns>
    public ClipboardContents ExcludeFromMonitorProcessing()
    {
        _excludeFromMonitoring = true;
        return this;
    }

    /// <summary>
    /// Place all formats and options with the token, the clipboard must be owned by the window of the token (cleared before).
    /// </summary>
    internal void PlaceOn(IClipboardAccessToken clipboardAccessToken)
    {
        foreach (var (_, place) in _formats)
        {
            place(clipboardAccessToken);
        }
        if (_canIncludeInHistory.HasValue || _canUploadToCloud.HasValue || _excludeFromMonitoring)
        {
            clipboardAccessToken.SetCloudClipboardOptions(_canIncludeInHistory, _canUploadToCloud, _excludeFromMonitoring);
        }
    }

    internal ClipboardContents Add(uint formatId, Action<IClipboardAccessToken> place)
    {
        if (formatId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(formatId), "0 is not a valid clipboard format.");
        }
        if (_formats.Any(f => f.FormatId == formatId))
        {
            throw new ArgumentException($"The clipboard format {ClipboardFormatExtensions.MapIdToFormat(formatId) ?? formatId.ToString()} was already added.", nameof(formatId));
        }
        _formats.Add((formatId, place));
        return this;
    }
}
