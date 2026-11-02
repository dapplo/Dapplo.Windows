// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// The metadata of a decoded frame, read once while decoding (a snapshot, no COM object is kept).
/// The paths are those of the WIC metadata query language, as WIC enumerates them, e.g. "/app1/ifd/{ushort=274}" for the orientation in a JPEG.
/// See <a href="https://learn.microsoft.com/en-us/windows/win32/wic/-wic-codec-metadataquerylanguage">the metadata query language</a> and <see cref="WicMetadataPaths"/>.
/// </summary>
public sealed class WicMetadata
{
    internal static readonly WicMetadata Empty = new WicMetadata(Array.Empty<KeyValuePair<string, object>>(), null, null, Array.Empty<WicMetadataRawBlock>());

    private readonly Dictionary<string, object> _lookup;

    internal WicMetadata(IList<KeyValuePair<string, object>> items, int? orientation, byte[] colorProfile, IList<WicMetadataRawBlock> rawBlocks)
    {
        Items = new ReadOnlyCollection<KeyValuePair<string, object>>(items);
        Orientation = orientation;
        ColorProfile = colorProfile;
        RawBlocks = new ReadOnlyCollection<WicMetadataRawBlock>(rawBlocks);
        _lookup = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items)
        {
            if (!_lookup.ContainsKey(item.Key))
            {
                _lookup.Add(item.Key, item.Value);
            }
        }
    }

    /// <summary>
    /// Every readable item of the frame, walked recursively through the nested readers: (query path, value).
    /// A nested reader itself is listed as a <see cref="WicMetadataBlock"/>, followed by its items.
    /// The values are string, string[], the integer types, float, double, bool, DateTime (UTC, from a FILETIME), Guid, byte[] (blob or vector of bytes)
    /// and arrays of numbers. EXIF rationals are ulong (RATIONAL) or long (SRATIONAL), see <see cref="TryGetRational"/>. Values of other types are left out.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object>> Items { get; }

    /// <summary>
    /// The EXIF orientation 1..8 (1 is normal, 6 is rotated 90 degrees clockwise to view), read with the path of the container or System.Photo.Orientation, or null
    /// </summary>
    public int? Orientation { get; }

    /// <summary>
    /// The ICC profile bytes of the first color context of the frame, or null
    /// </summary>
    public byte[] ColorProfile { get; }

    /// <summary>
    /// The metadata blocks as raw bytes, as far as the metadata handler can save them, to carry them over unchanged with <see cref="WicMetadataWriter.AddRawBlock"/>
    /// </summary>
    public IReadOnlyList<WicMetadataRawBlock> RawBlocks { get; }

    /// <summary>
    /// Get the value of a path as it is listed in <see cref="Items"/>, e.g. "/app1/ifd/{ushort=274}" (case insensitive)
    /// </summary>
    /// <param name="query">the path</param>
    /// <param name="value">the value, see <see cref="Items"/> for the types</param>
    /// <returns>true when the frame has the path</returns>
    public bool TryGet(string query, out object value)
    {
        if (query == null)
        {
            value = null;
            return false;
        }
        return _lookup.TryGetValue(query, out value);
    }

    /// <summary>
    /// Get an EXIF rational (stored by WIC as VT_UI8 or VT_I8 with the numerator in the low 32 bits)
    /// </summary>
    public bool TryGetRational(string query, out WicRational value)
    {
        if (TryGet(query, out var raw))
        {
            switch (raw)
            {
                case ulong packed:
                    value = WicRational.FromPacked(packed);
                    return true;
                case long signedPacked:
                    value = WicRational.FromPacked(unchecked((ulong)signedPacked));
                    return true;
            }
        }
        value = default;
        return false;
    }
}

/// <summary>
/// A nested metadata reader (a block like "/app1" or "/app1/ifd/exif"), as a value in <see cref="WicMetadata.Items"/>
/// </summary>
public sealed class WicMetadataBlock
{
    internal WicMetadataBlock(string path, Guid format)
    {
        Path = path;
        Format = format;
    }

    /// <summary>
    /// The path of the block
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The metadata format of the block, see <see cref="WicMetadataFormats"/>
    /// </summary>
    public Guid Format { get; }

    /// <inheritdoc />
    public override string ToString() => $"Block {Path} ({Format})";
}

/// <summary>
/// The raw bytes of a metadata block
/// </summary>
public sealed class WicMetadataRawBlock
{
    /// <summary>
    /// Create a raw block, e.g. to add it with <see cref="WicMetadataWriter.AddRawBlock"/>
    /// </summary>
    /// <param name="path">the query path of the block, e.g. "/app1" or "/xmp", null when unknown</param>
    /// <param name="format">the metadata format, see <see cref="WicMetadataFormats"/></param>
    /// <param name="data">the bytes as the metadata handler persists them</param>
    public WicMetadataRawBlock(string path, Guid format, byte[] data)
    {
        Path = path;
        Format = format;
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    /// <summary>
    /// The query path of the block, e.g. "/app1" or "/ifd/xmp"
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The metadata format, see <see cref="WicMetadataFormats"/>
    /// </summary>
    public Guid Format { get; }

    /// <summary>
    /// The bytes as the metadata handler persists them (IWICPersistStream.SaveEx)
    /// </summary>
    public byte[] Data { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Path} ({Format}): {Data.Length} bytes";
}

/// <summary>
/// Metadata which an encoder didn't accept, see <see cref="WicEncodeResult.MetadataIssues"/>
/// </summary>
public sealed class WicMetadataIssue
{
    internal WicMetadataIssue(string query, int errorCode, string message)
    {
        Query = query;
        ErrorCode = errorCode;
        Message = message;
    }

    /// <summary>
    /// The query path, or a description like "color profile" or "raw block /xmp"
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// The HRESULT
    /// </summary>
    public int ErrorCode { get; }

    /// <summary>
    /// What failed
    /// </summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Query}: {Message} (0x{ErrorCode:X8})";
}
