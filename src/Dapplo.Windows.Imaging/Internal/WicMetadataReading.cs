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

    public static WicMetadata Read(IWICImagingFactory factory, IWICBitmapFrameDecode frame, Guid containerFormat)
    {
        var items = new List<KeyValuePair<string, object>>();
        var rawBlocks = new List<WicMetadataRawBlock>();
        int? orientation = null;

        IWICMetadataQueryReader queryReader = null;
        try
        {
            if (frame.GetMetadataQueryReader(out queryReader).Succeeded() && queryReader != null)
            {
                Walk(queryReader, string.Empty, items, rawBlocks, 0);
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

        var colorProfile = ReadColorProfile(factory, frame);
        return new WicMetadata(items, orientation, colorProfile, rawBlocks);
    }

    private static void Walk(IWICMetadataQueryReader reader, string prefix, List<KeyValuePair<string, object>> items, List<WicMetadataRawBlock> rawBlocks, int depth)
    {
        if (depth > MaxDepth || !reader.GetEnumerator(out var enumerator).Succeeded() || enumerator == null)
        {
            return;
        }
        try
        {
            var names = new string[1];
            while (enumerator.Next(1, names, IntPtr.Zero) == 0 && names[0] != null)
            {
                var name = names[0];
                names[0] = null;
                var path = prefix + name;
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
                        Log.Debug().WriteLine("Metadata {0} has the unsupported type {1}, skipping it", path, PropVariant.GetVarType(propVariant));
                        continue;
                    }
                    if (value != null && Marshal.IsComObject(value))
                    {
                        if (value is IWICMetadataQueryReader nested)
                        {
                            nested.GetContainerFormat(out var format);
                            items.Add(new KeyValuePair<string, object>(path, new WicMetadataBlock(path, format)));
                            TryReadRawBlock(value, path, format, rawBlocks);
                            Walk(nested, path, items, rawBlocks, depth + 1);
                        }
                        continue;
                    }
                    items.Add(new KeyValuePair<string, object>(path, value));
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
    /// Save the block with IWICPersistStream, when the nested reader supports it
    /// </summary>
    private static void TryReadRawBlock(object nestedReader, string path, Guid format, List<WicMetadataRawBlock> rawBlocks)
    {
        if (nestedReader is not IWICPersistStream persistStream)
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
