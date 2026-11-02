// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Internal;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// Writes the metadata of the frame which is encoded, see <see cref="WicEncodeOptions.Metadata"/>.
/// Everything the encoder doesn't accept is skipped and reported in <see cref="Issues"/>, it never fails the encoding.
/// Only valid during the callback.
/// </summary>
public sealed class WicMetadataWriter
{
    private static readonly LogSource Log = new LogSource();

    private readonly IWICImagingFactory _factory;
    private readonly IWICBitmapFrameEncode _frame;
    private readonly IWICMetadataQueryWriter _queryWriter;
    private readonly List<WicMetadataIssue> _issues = new List<WicMetadataIssue>();
    private bool _closed;

    internal WicMetadataWriter(IWICImagingFactory factory, IWICBitmapFrameEncode frame, IWICMetadataQueryWriter queryWriter, Guid containerFormat, HResult queryWriterResult)
    {
        _factory = factory;
        _frame = frame;
        _queryWriter = queryWriter;
        ContainerFormat = containerFormat;
        QueryWriterResult = queryWriterResult;
    }

    /// <summary>
    /// The container format which is written, to pick the paths with <see cref="WicMetadataPaths"/>
    /// </summary>
    public Guid ContainerFormat { get; }

    /// <summary>
    /// True when the encoder has a metadata query writer, without one <see cref="Set"/> always fails
    /// </summary>
    public bool SupportsQueries => _queryWriter != null;

    private HResult QueryWriterResult { get; }

    /// <summary>
    /// What was skipped so far
    /// </summary>
    public IReadOnlyList<WicMetadataIssue> Issues => _issues;

    internal void Close() => _closed = true;

    private void EnsureOpen()
    {
        if (_closed)
        {
            throw new InvalidOperationException("The metadata writer can only be used in the WicEncodeOptions.Metadata callback");
        }
    }

    private void AddIssue(string query, HResult hResult, string message)
    {
        Log.Info().WriteLine("Metadata {0} was not written: {1} (0x{2:X8})", query, message, (int)hResult);
        _issues.Add(new WicMetadataIssue(query, (int)hResult, message));
    }

    /// <summary>
    /// Set a value with the metadata query language, e.g. Set(WicMetadataPaths.Software(format), WicMetadataValue.AnsiString("Greenshot")) or Set("/xmp/dc:title", "Title").
    /// Supported values: string (VT_LPWSTR), <see cref="WicMetadataValue"/> (VT_LPSTR or a vector of bytes), bool, the integer types, float, double,
    /// <see cref="WicRational"/>, <see cref="WicSignedRational"/>, DateTime and DateTimeOffset (FILETIME), Guid, byte[] (VT_BLOB), ushort[], uint[], int[] and string[].
    /// Parent blocks which don't exist yet are created by WIC.
    /// </summary>
    /// <param name="query">the path, see <see cref="WicMetadataPaths"/></param>
    /// <param name="value">the value</param>
    /// <returns>true when the encoder accepted it, otherwise it is in <see cref="Issues"/></returns>
    /// <exception cref="ArgumentException">when the type of the value is not supported</exception>
    public bool Set(string query, object value)
    {
        EnsureOpen();
        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentNullException(nameof(query));
        }
        if (_queryWriter == null)
        {
            AddIssue(query, QueryWriterResult, "The encoder has no metadata writer");
            return false;
        }
        var propVariant = PropVariant.Allocate();
        try
        {
            PropVariant.Write(propVariant, value);
            var hResult = _queryWriter.SetMetadataByName(query, propVariant);
            if (!hResult.Succeeded())
            {
                AddIssue(query, hResult, "The encoder didn't accept the value");
                return false;
            }
            return true;
        }
        finally
        {
            PropVariant.Free(propVariant);
        }
    }

    /// <summary>
    /// Remove a value which the encoder would write, e.g. one it adds itself
    /// </summary>
    /// <returns>true when it was removed</returns>
    public bool Remove(string query)
    {
        EnsureOpen();
        if (_queryWriter == null)
        {
            AddIssue(query, QueryWriterResult, "The encoder has no metadata writer");
            return false;
        }
        var hResult = _queryWriter.RemoveMetadataByName(query);
        if (!hResult.Succeeded())
        {
            AddIssue(query, hResult, "The value couldn't be removed");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Write an ICC profile as color context of the frame
    /// </summary>
    /// <param name="icc">the bytes of the ICC profile</param>
    /// <returns>true when the encoder accepted it</returns>
    public bool SetColorProfile(byte[] icc)
    {
        EnsureOpen();
        if (icc == null || icc.Length == 0)
        {
            throw new ArgumentNullException(nameof(icc));
        }
        IWICColorContext colorContext = null;
        var pointer = IntPtr.Zero;
        var handle = GCHandle.Alloc(icc, GCHandleType.Pinned);
        try
        {
            var hResult = _factory.CreateColorContext(out colorContext);
            if (hResult.Succeeded())
            {
                hResult = colorContext.InitializeFromMemory(handle.AddrOfPinnedObject(), (uint)icc.Length);
            }
            if (hResult.Succeeded())
            {
                pointer = Marshal.GetComInterfaceForObject(colorContext, typeof(IWICColorContext));
                var pointers = new[] { pointer };
                var pointersHandle = GCHandle.Alloc(pointers, GCHandleType.Pinned);
                try
                {
                    hResult = _frame.SetColorContexts(1, pointersHandle.AddrOfPinnedObject());
                }
                finally
                {
                    pointersHandle.Free();
                }
            }
            if (!hResult.Succeeded())
            {
                AddIssue("color profile", hResult, "The encoder didn't accept the color profile");
                return false;
            }
            return true;
        }
        finally
        {
            handle.Free();
            if (pointer != IntPtr.Zero)
            {
                Marshal.Release(pointer);
            }
            Wic.Release(colorContext);
        }
    }

    /// <summary>
    /// Write a metadata block from its raw bytes, e.g. a <see cref="WicMetadataRawBlock"/> of a decoded image, to carry it over unchanged.
    /// With a path (e.g. "/xmp" or "/ifd/xmp") the block is set at that path, without a path it is added as top-level block of the frame.
    /// </summary>
    /// <param name="block">the raw block</param>
    /// <returns>true when the encoder accepted it</returns>
    public bool AddRawBlock(WicMetadataRawBlock block)
    {
        EnsureOpen();
        if (block == null)
        {
            throw new ArgumentNullException(nameof(block));
        }
        var description = $"raw block {block.Path ?? block.Format.ToString()}";
        IWICComponentFactory componentFactory = _factory as IWICComponentFactory;
        IWICMetadataReader reader = null;
        IWICMetadataWriter writer = null;
        try
        {
            if (componentFactory == null)
            {
                AddIssue(description, HResult.E_NOINTERFACE, "The factory has no IWICComponentFactory");
                return false;
            }
            using var memoryStream = new MemoryStream(block.Data, false);
            var format = block.Format;
            var hResult = componentFactory.CreateMetadataReader(ref format, IntPtr.Zero, 0, new ManagedIStream(memoryStream), out reader);
            if (hResult.Succeeded())
            {
                hResult = componentFactory.CreateMetadataWriterFromReader(reader, IntPtr.Zero, out writer);
            }
            if (!hResult.Succeeded())
            {
                AddIssue(description, hResult, "The block couldn't be read");
                return false;
            }

            if (block.Path != null && _queryWriter != null)
            {
                var propVariant = PropVariant.Allocate();
                try
                {
                    PropVariant.WriteUnknown(propVariant, writer, typeof(IWICMetadataWriter));
                    hResult = _queryWriter.SetMetadataByName(block.Path, propVariant);
                }
                finally
                {
                    PropVariant.Free(propVariant);
                }
                if (hResult.Succeeded())
                {
                    return true;
                }
                // A top level path can also be added as block
                if (block.Path.LastIndexOf('/') > 0)
                {
                    AddIssue(description, hResult, "The encoder didn't accept the block at this path");
                    return false;
                }
            }

            if (_frame is not IWICMetadataBlockWriter blockWriter)
            {
                AddIssue(description, HResult.E_NOINTERFACE, "The encoder has no metadata block writer");
                return false;
            }
            hResult = blockWriter.AddWriter(writer);
            if (!hResult.Succeeded())
            {
                AddIssue(description, hResult, "The encoder didn't accept the block");
                return false;
            }
            return true;
        }
        finally
        {
            Wic.Release(writer);
            Wic.Release(reader);
        }
    }
}
