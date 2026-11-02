// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// Finds out whether a file stores a resolution, or whether the resolution of IWICBitmapSource::GetResolution is only the default of the decoder.
/// WIC has no way to say "unknown", the rules per container are kept here.
/// The metadata of the frame is used first, the bytes of the file only where WIC doesn't expose the field.
/// </summary>
internal static class ResolutionProbe
{
    private static readonly LogSource Log = new LogSource();

    private const int TagXResolution = 282;
    private const int TagResolutionUnit = 296;

    /// <summary>
    /// JPEG XR (ITU-T T.832, Annex A) WIDTH_RESOLUTION, a FLOAT in dots per inch
    /// </summary>
    private const int JpegXrTagWidthResolution = 0xBC82;

    /// <param name="queryReader">the query reader of the frame, can be null</param>
    /// <param name="metadata">the metadata items of the frame</param>
    /// <param name="containerFormat">the container format of the decoder</param>
    /// <param name="input">the seekable stream the decoder reads, its position is restored</param>
    /// <param name="origin">the position of the file in the stream</param>
    public static bool HasResolution(IWICMetadataQueryReader queryReader, WicMetadata metadata, Guid containerFormat, Stream input, long origin)
    {
        try
        {
            if (containerFormat == WicContainerFormats.Png)
            {
                return Png(queryReader, input, origin);
            }
            if (containerFormat == WicContainerFormats.Jpeg)
            {
                return Jpeg(queryReader);
            }
            if (containerFormat == WicContainerFormats.Tiff || containerFormat == WicContainerFormats.Adng)
            {
                return IfdResolution(queryReader, "/ifd");
            }
            if (containerFormat == WicContainerFormats.Wmp)
            {
                return JpegXr(input, origin);
            }
            if (containerFormat == WicContainerFormats.Bmp)
            {
                return Bmp(input, origin);
            }
            if (containerFormat == WicContainerFormats.Gif || containerFormat == WicContainerFormats.Ico || containerFormat == WicContainerFormats.Dds)
            {
                // GIF only has a pixel aspect ratio, ICO and DDS have nothing
                return false;
            }
            return ExifResolution(metadata);
        }
        catch (Exception ex) when (ex is IOException || ex is NotSupportedException || ex is ObjectDisposedException || ex is InvalidCastException)
        {
            Log.Debug().WriteLine("Couldn't find out whether the {0} file stores a resolution: {1}", containerFormat, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// PNG: a pHYs chunk with the unit specifier 1 (meter); unit 0 is only an aspect ratio
    /// </summary>
    private static bool Png(IWICMetadataQueryReader queryReader, Stream input, long origin)
    {
        if (TryQueryInteger(queryReader, "/pHYs/UnitSpecifier", out var unit))
        {
            return unit == 1 && TryQueryInteger(queryReader, "/pHYs/PixelsPerUnitX", out var pixelsPerUnit) ? pixelsPerUnit > 0 : unit == 1;
        }
        // The reader doesn't expose pHYs, read the chunk headers up to the first IDAT
        return WithPosition(input, () =>
        {
            input.Position = origin + 8;
            var header = new byte[8];
            for (int chunk = 0; chunk < 1000; chunk++)
            {
                if (!ReadFully(input, header, 8))
                {
                    return false;
                }
                var length = ReadUInt32BigEndian(header, 0);
                var type = System.Text.Encoding.ASCII.GetString(header, 4, 4);
                if (type == "pHYs")
                {
                    var data = new byte[9];
                    return length >= 9 && ReadFully(input, data, 9) && data[8] == 1 && ReadUInt32BigEndian(data, 0) > 0;
                }
                if (type == "IDAT" || type == "IEND")
                {
                    return false;
                }
                // Skip the data and the CRC
                input.Position += length + 4L;
            }
            return false;
        });
    }

    /// <summary>
    /// JPEG: the JFIF density unit 1 (inch) or 2 (cm), or an EXIF XResolution with a ResolutionUnit other than 1 (none)
    /// </summary>
    private static bool Jpeg(IWICMetadataQueryReader queryReader)
    {
        if (TryQueryInteger(queryReader, "/app0/{ushort=1}", out var units) && (units == 1 || units == 2))
        {
            return true;
        }
        return IfdResolution(queryReader, "/app1/ifd");
    }

    /// <summary>
    /// TIFF IFD: XResolution present and ResolutionUnit not 1 (none); without ResolutionUnit the unit is inch
    /// </summary>
    private static bool IfdResolution(IWICMetadataQueryReader queryReader, string ifdPath)
    {
        if (!TryQuery(queryReader, $"{ifdPath}/{{ushort={TagXResolution}}}", out _))
        {
            return false;
        }
        return !TryQueryInteger(queryReader, $"{ifdPath}/{{ushort={TagResolutionUnit}}}", out var unit) || unit != 1;
    }

    /// <summary>
    /// Other containers (WebP, HEIF, JPEG XL, RAW, codecs of other vendors): an EXIF IFD (IFD0) with XResolution and a ResolutionUnit other than 1.
    /// The path of the IFD differs per container, the IFD blocks are found in the enumerated metadata.
    /// </summary>
    private static bool ExifResolution(WicMetadata metadata)
    {
        foreach (var item in metadata.Items)
        {
            if (item.Value is not WicMetadataBlock block || block.Format != WicMetadataFormats.Ifd)
            {
                continue;
            }
            if (!metadata.TryGet($"{block.Path}/{{ushort={TagXResolution}}}", out _))
            {
                continue;
            }
            if (!metadata.TryGet($"{block.Path}/{{ushort={TagResolutionUnit}}}", out var unit) || unit is not IConvertible convertible || convertible.ToInt64(null) != 1)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// JPEG XR: the WIDTH_RESOLUTION tag in the IFD of the container, read from the file (the WIC metadata reader of JPEG XR doesn't expose it)
    /// </summary>
    private static bool JpegXr(Stream input, long origin) => WithPosition(input, () =>
    {
        input.Position = origin;
        var header = new byte[8];
        // "II", 0xBC, version
        if (!ReadFully(input, header, 8) || header[0] != 0x49 || header[1] != 0x49 || header[2] != 0xBC)
        {
            return false;
        }
        input.Position = origin + ReadUInt32LittleEndian(header, 4);
        var countBytes = new byte[2];
        if (!ReadFully(input, countBytes, 2))
        {
            return false;
        }
        var count = countBytes[0] | countBytes[1] << 8;
        var entry = new byte[12];
        for (int i = 0; i < count; i++)
        {
            if (!ReadFully(input, entry, 12))
            {
                return false;
            }
            var tag = entry[0] | entry[1] << 8;
            if (tag != JpegXrTagWidthResolution)
            {
                continue;
            }
            var type = entry[2] | entry[3] << 8;
            // FLOAT (11), stored in the value field
            return type == 11 ? BitConverter.ToSingle(entry, 8) > 0 : ReadUInt32LittleEndian(entry, 8) > 0;
        }
        return false;
    });

    /// <summary>
    /// BMP: biXPelsPerMeter of the BITMAPINFOHEADER (byte 38 of the file), 0 means not stored. A BITMAPCOREHEADER has no resolution.
    /// </summary>
    private static bool Bmp(Stream input, long origin) => WithPosition(input, () =>
    {
        input.Position = origin;
        var header = new byte[42];
        if (!ReadFully(input, header, header.Length) || header[0] != (byte)'B' || header[1] != (byte)'M')
        {
            return false;
        }
        var infoHeaderSize = ReadUInt32LittleEndian(header, 14);
        return infoHeaderSize >= 40 && ReadUInt32LittleEndian(header, 38) != 0;
    });

    private static bool TryQuery(IWICMetadataQueryReader queryReader, string query, out object value)
    {
        value = null;
        if (queryReader == null)
        {
            return false;
        }
        var propVariant = PropVariant.Allocate();
        try
        {
            if (!queryReader.GetMetadataByName(query, propVariant).Succeeded() || !PropVariant.TryRead(propVariant, out value))
            {
                return false;
            }
            if (value != null && System.Runtime.InteropServices.Marshal.IsComObject(value))
            {
                Wic.Release(value);
                value = null;
                return true;
            }
            return value != null;
        }
        finally
        {
            PropVariant.Free(propVariant);
        }
    }

    private static bool TryQueryInteger(IWICMetadataQueryReader queryReader, string query, out long value)
    {
        value = 0;
        if (!TryQuery(queryReader, query, out var raw) || raw is not IConvertible convertible || raw is string)
        {
            return false;
        }
        value = convertible.ToInt64(null);
        return true;
    }

    private static bool WithPosition(Stream input, Func<bool> read)
    {
        if (input == null || !input.CanSeek)
        {
            return false;
        }
        var position = input.Position;
        try
        {
            return read();
        }
        finally
        {
            input.Position = position;
        }
    }

    private static bool ReadFully(Stream input, byte[] buffer, int count)
    {
        var offset = 0;
        while (offset < count)
        {
            var read = input.Read(buffer, offset, count - offset);
            if (read <= 0)
            {
                return false;
            }
            offset += read;
        }
        return true;
    }

    private static uint ReadUInt32BigEndian(byte[] data, int offset) => (uint)(data[offset] << 24 | data[offset + 1] << 16 | data[offset + 2] << 8 | data[offset + 3]);

    private static uint ReadUInt32LittleEndian(byte[] data, int offset) => (uint)(data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24);
}
