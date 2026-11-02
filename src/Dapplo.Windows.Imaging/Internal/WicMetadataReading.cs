// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Dapplo.Log;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging.Internal;

/// <summary>
/// Reads the metadata of a decoded frame into a <see cref="WicMetadata"/> snapshot
/// </summary>
internal static class WicMetadataReading
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    /// Nested readers deeper than this are not walked (protection against broken files)
    /// </summary>
    private const int MaxDepth = 16;

    /// <summary>
    /// The names the metadata query language has for the nested blocks which WIC enumerates by their tag, e.g. "/app1/{ushort=0}" is "/app1/ifd"
    /// </summary>
    private static readonly Dictionary<Guid, string> BlockAliases = new Dictionary<Guid, string>
    {
        { WicMetadataFormats.Ifd, "ifd" },
        { WicMetadataFormats.Exif, "exif" },
        { WicMetadataFormats.Gps, "gps" },
        { new Guid("ED686F8E-681F-4C8B-BD41-A8ADDBF6B3FC"), "interop" },
        { WicMetadataFormats.Xmp, "xmp" },
        { WicMetadataFormats.Iptc, "iptc" },
        { new Guid("16100D66-8570-4BB9-B92D-FDA4B23ECE67"), "irb" },
        { new Guid("243DCEE9-8703-40EE-8EF0-22A600B8058C"), "thumb" },
        { WicMetadataFormats.App1, "app1" },
        { WicMetadataFormats.App13, "app13" },
        { new Guid("79007028-268D-45D6-A3C2-354E6A504BC9"), "app0" },
    };

    /// <summary>
    /// The top-level blocks which are kept as raw bytes: the self-contained EXIF, XMP and IPTC blocks, not the image structure of a TIFF IFD or the JPEG tables
    /// </summary>
    private static readonly HashSet<Guid> RawTopLevelFormats = new HashSet<Guid> { WicMetadataFormats.App1, WicMetadataFormats.App13, WicMetadataFormats.Xmp, WicMetadataFormats.Iptc };

    /// <summary>
    /// The nested blocks which are kept as raw bytes, e.g. the XMP packet in the IFD of a TIFF
    /// </summary>
    private static readonly HashSet<Guid> RawNestedFormats = new HashSet<Guid> { WicMetadataFormats.Xmp, WicMetadataFormats.Iptc };

    public static WicMetadata Read(IWICImagingFactory factory, IWICBitmapFrameDecode frame, Guid containerFormat)
    {
        var items = new List<KeyValuePair<string, object>>();
        var aliases = new List<KeyValuePair<string, object>>();
        int? orientation = null;

        IWICMetadataQueryReader queryReader = null;
        try
        {
            if (frame.GetMetadataQueryReader(out queryReader).Succeeded() && queryReader != null)
            {
                Walk(queryReader, string.Empty, string.Empty, items, aliases, 0);
                orientation = ReadOrientation(queryReader, containerFormat);
            }
        }
        catch (Exception ex) when (ex is COMException || ex is InvalidCastException)
        {
            Log.Warn().WriteLine(ex, "Reading the metadata failed, keeping {0} items", items.Count);
        }
        finally
        {
            Wic.Release(queryReader);
        }

        var rawBlocks = ReadRawBlocks(frame, containerFormat);
        var colorProfile = ReadColorProfile(factory, frame);
        return new WicMetadata(items, aliases, orientation, colorProfile, rawBlocks);
    }

    /// <summary>
    /// Walk a query reader, the items get the friendly path (e.g. "/app1/ifd/exif/{ushort=36867}"), the path as WIC enumerates it (e.g. "/app1/{ushort=0}/{ushort=34665}/{ushort=36867}") is an alias for the lookup
    /// </summary>
    private static void Walk(IWICMetadataQueryReader reader, string prefix, string rawPrefix, List<KeyValuePair<string, object>> items, List<KeyValuePair<string, object>> aliases, int depth)
    {
        if (depth > MaxDepth || !reader.GetEnumerator(out var enumerator).Succeeded() || enumerator == null)
        {
            return;
        }
        var usedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var names = new string[1];
            while (enumerator.Next(1, names, IntPtr.Zero) == 0 && names[0] != null)
            {
                var name = names[0];
                names[0] = null;
                var rawPath = rawPrefix + name;
                var propVariant = PropVariant.Allocate();
                object value = null;
                try
                {
                    if (!reader.GetMetadataByName(name, propVariant).Succeeded())
                    {
                        continue;
                    }
                    if (!PropVariant.TryRead(propVariant, out value))
                    {
                        Log.Debug().WriteLine("Metadata {0} has the unsupported type {1}, skipping it", rawPath, PropVariant.GetVarType(propVariant));
                        continue;
                    }
                    if (value != null && Marshal.IsComObject(value))
                    {
                        if (value is IWICMetadataQueryReader nested)
                        {
                            nested.GetContainerFormat(out var format);
                            var friendlyName = name;
                            // Only a tag like "/{ushort=34665}" gets the name of its block, and only once per level
                            if (name.StartsWith("/{", StringComparison.Ordinal) && BlockAliases.TryGetValue(format, out var alias) && usedAliases.Add(alias))
                            {
                                friendlyName = "/" + alias;
                            }
                            var path = prefix + friendlyName;
                            var block = new WicMetadataBlock(path, format);
                            items.Add(new KeyValuePair<string, object>(path, block));
                            if (!string.Equals(path, rawPath, StringComparison.OrdinalIgnoreCase))
                            {
                                aliases.Add(new KeyValuePair<string, object>(rawPath, block));
                            }
                            Walk(nested, path, rawPath, items, aliases, depth + 1);
                        }
                        continue;
                    }
                    var itemPath = prefix + name;
                    items.Add(new KeyValuePair<string, object>(itemPath, value));
                    if (!string.Equals(itemPath, rawPath, StringComparison.OrdinalIgnoreCase))
                    {
                        aliases.Add(new KeyValuePair<string, object>(rawPath, value));
                    }
                }
                finally
                {
                    Wic.Release(value);
                    PropVariant.Free(propVariant);
                }
            }
        }
        finally
        {
            Wic.Release(enumerator);
        }
    }

    /// <summary>
    /// The EXIF, XMP and IPTC blocks as raw bytes: the top-level blocks of IWICMetadataBlockReader and the XMP / IPTC blocks nested in them
    /// </summary>
    private static List<WicMetadataRawBlock> ReadRawBlocks(IWICBitmapFrameDecode frame, Guid containerFormat)
    {
        var rawBlocks = new List<WicMetadataRawBlock>();
        if (frame is not IWICMetadataBlockReader blockReader || !blockReader.GetCount(out var count).Succeeded())
        {
            return rawBlocks;
        }
        // In other containers than TIFF and JPEG XR the top-level IFD is only metadata (e.g. the EXIF of HEIF)
        var ifdIsMetadata = containerFormat != WicContainerFormats.Tiff && containerFormat != WicContainerFormats.Wmp;
        for (uint i = 0; i < count; i++)
        {
            IWICMetadataReader reader = null;
            try
            {
                if (!blockReader.GetReaderByIndex(i, out reader).Succeeded() || reader == null || !reader.GetMetadataFormat(out var format).Succeeded())
                {
                    continue;
                }
                var path = BlockAliases.TryGetValue(format, out var alias) ? "/" + alias : null;
                if (RawTopLevelFormats.Contains(format) || ifdIsMetadata && format == WicMetadataFormats.Ifd)
                {
                    AddRawBlock(reader, path, format, rawBlocks);
                }
                if (path != null)
                {
                    WalkRawBlocks(reader, path, rawBlocks, 0);
                }
            }
            catch (COMException ex)
            {
                Log.Debug().WriteLine("Couldn't read the metadata block {0}: {1}", i, ex.Message);
            }
            finally
            {
                Wic.Release(reader);
            }
        }
        return rawBlocks;
    }

    /// <summary>
    /// Find the XMP and IPTC blocks nested in a block, e.g. tag 700 in the IFD of a TIFF
    /// </summary>
    private static void WalkRawBlocks(IWICMetadataReader reader, string path, List<WicMetadataRawBlock> rawBlocks, int depth)
    {
        if (depth > MaxDepth || !reader.GetCount(out var count).Succeeded())
        {
            return;
        }
        for (uint i = 0; i < count; i++)
        {
            var id = PropVariant.Allocate();
            var value = PropVariant.Allocate();
            object nestedObject = null;
            try
            {
                if (!reader.GetValueByIndex(i, IntPtr.Zero, id, value).Succeeded() || PropVariant.GetVarType(value) != 13 || !PropVariant.TryRead(value, out nestedObject))
                {
                    continue;
                }
                if (nestedObject is not IWICMetadataReader nested || !nested.GetMetadataFormat(out var format).Succeeded())
                {
                    continue;
                }
                var name = BlockAliases.TryGetValue(format, out var alias) ? alias : PropVariant.TryRead(id, out var idValue) && idValue is ushort tag ? $"{{ushort={tag}}}" : null;
                if (name == null)
                {
                    continue;
                }
                var nestedPath = $"{path}/{name}";
                if (RawNestedFormats.Contains(format))
                {
                    AddRawBlock(nested, nestedPath, format, rawBlocks);
                }
                WalkRawBlocks(nested, nestedPath, rawBlocks, depth + 1);
            }
            finally
            {
                Wic.Release(nestedObject);
                PropVariant.Free(id);
                PropVariant.Free(value);
            }
        }
    }

    /// <summary>
    /// Save the block with IWICPersistStream
    /// </summary>
    private static void AddRawBlock(IWICMetadataReader reader, string path, Guid format, List<WicMetadataRawBlock> rawBlocks)
    {
        if (reader is not IWICPersistStream persistStream)
        {
            return;
        }
        try
        {
            using var memoryStream = new MemoryStream();
            var stream = new ManagedIStream(memoryStream);
            if (persistStream.SaveEx(stream, 0, 0).Succeeded())
            {
                rawBlocks.Add(new WicMetadataRawBlock(path, format, memoryStream.ToArray()));
            }
        }
        catch (Exception ex) when (ex is COMException || ex is IOException)
        {
            Log.Debug().WriteLine("Couldn't save the metadata block {0}: {1}", path, ex.Message);
        }
    }

    private static int? ReadOrientation(IWICMetadataQueryReader reader, Guid containerFormat)
    {
        foreach (var query in WicMetadataPaths.OrientationQueries(containerFormat))
        {
            var propVariant = PropVariant.Allocate();
            try
            {
                if (reader.GetMetadataByName(query, propVariant).Succeeded() && PropVariant.TryRead(propVariant, out var value) && value != null && value is IConvertible)
                {
                    var orientation = Convert.ToInt32(value);
                    if (orientation >= 1 && orientation <= 8)
                    {
                        return orientation;
                    }
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidCastException || ex is OverflowException)
            {
                // Not a number
            }
            finally
            {
                PropVariant.Free(propVariant);
            }
        }
        return null;
    }

    private static byte[] ReadColorProfile(IWICImagingFactory factory, IWICBitmapFrameDecode frame)
    {
        if (!frame.GetColorContexts(0, IntPtr.Zero, out var count).Succeeded() || count == 0)
        {
            return null;
        }
        var contexts = new IWICColorContext[count];
        var pointers = new nint[count];
        try
        {
            for (int i = 0; i < count; i++)
            {
                Wic.Check(factory.CreateColorContext(out contexts[i]), "IWICImagingFactory.CreateColorContext");
                pointers[i] = Marshal.GetComInterfaceForObject(contexts[i], typeof(IWICColorContext));
            }
            var handle = GCHandle.Alloc(pointers, GCHandleType.Pinned);
            try
            {
                if (!frame.GetColorContexts(count, handle.AddrOfPinnedObject(), out count).Succeeded())
                {
                    return null;
                }
            }
            finally
            {
                handle.Free();
            }
            for (int i = 0; i < count; i++)
            {
                // 1 = WICColorContextProfile
                if (!contexts[i].GetType(out var type).Succeeded() || type != 1)
                {
                    continue;
                }
                if (!contexts[i].GetProfileBytes(0, IntPtr.Zero, out var size).Succeeded() || size == 0)
                {
                    continue;
                }
                var profile = new byte[size];
                var profileHandle = GCHandle.Alloc(profile, GCHandleType.Pinned);
                try
                {
                    if (contexts[i].GetProfileBytes(size, profileHandle.AddrOfPinnedObject(), out size).Succeeded())
                    {
                        return profile;
                    }
                }
                finally
                {
                    profileHandle.Free();
                }
            }
            return null;
        }
        catch (COMException ex)
        {
            Log.Warn().WriteLine(ex, "Reading the color profile failed");
            return null;
        }
        finally
        {
            foreach (var pointer in pointers)
            {
                if (pointer != IntPtr.Zero)
                {
                    Marshal.Release(pointer);
                }
            }
            foreach (var context in contexts)
            {
                Wic.Release(context);
            }
        }
    }
}
