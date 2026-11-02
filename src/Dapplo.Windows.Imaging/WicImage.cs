// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Log;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Imaging.Internal;
using Dapplo.Windows.Imaging.Interop;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// Encode and decode images with the Windows Imaging Component (WIC), without WPF, System.Drawing or other pixel libraries:
/// pixels are passed as memory (pointer and stride) or row by row through a callback, streams are .NET streams.
/// Every COM object is created per call and released before the call returns, the methods work from STA and MTA threads.
/// When WIC has no codec for the container format or the stream, a <see cref="WicCodecNotFoundException"/> is thrown.
/// </summary>
public static class WicImage
{
    private static readonly LogSource Log = new LogSource();

    /// <summary>
    /// Rows per CopyPixels call when decoding through a <see cref="WicRowConsumer"/> are chosen to stay around this size
    /// </summary>
    private const int BandBytes = 4 * 1024 * 1024;

    #region Encoding

    /// <summary>
    /// Encode pixels in memory, e.g. GDI+ BitmapData.Scan0 or a contiguous ImageSharp buffer. The pixels are read without a copy of the image.
    /// </summary>
    /// <param name="destination">the stream to write to, from its current position; a stream which can't seek is written through a MemoryStream</param>
    /// <param name="options">the container format and the encoder options</param>
    /// <param name="pixels">pointer to the first row</param>
    /// <param name="width">width in pixels</param>
    /// <param name="height">height in pixels</param>
    /// <param name="stride">bytes from the start of one row to the next, at least width * bits per pixel / 8 (negative strides are not supported)</param>
    /// <param name="pixelFormat">the pixel format of the memory, see <see cref="WicPixelFormats"/></param>
    /// <returns>WicEncodeResult with the pixel format the encoder used and the skipped metadata</returns>
    public static WicEncodeResult Encode(Stream destination, WicEncodeOptions options, IntPtr pixels, int width, int height, int stride, Guid pixelFormat)
    {
        if (pixels == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(pixels));
        }
        ValidateEncode(destination, options, width, height);
        return Encode(destination, options, factory =>
        {
            var bitsPerPixel = Wic.GetBitsPerPixel(factory, pixelFormat);
            if (stride < Wic.RowBytes(width, bitsPerPixel))
            {
                throw new ArgumentOutOfRangeException(nameof(stride), stride, "The stride is smaller than a row");
            }
            return new WicMemoryPixelSource(pixels, width, height, stride, pixelFormat, bitsPerPixel, options.DpiX, options.DpiY);
        });
    }

    /// <summary>
    /// Encode pixels which are supplied row by row, e.g. from ImageSharp ProcessPixelRows when the image isn't contiguous.
    /// The encoder pulls the rows it needs, no copy of the image is made.
    /// </summary>
    /// <param name="destination">the stream to write to, from its current position; a stream which can't seek is written through a MemoryStream</param>
    /// <param name="options">the container format and the encoder options</param>
    /// <param name="width">width in pixels</param>
    /// <param name="height">height in pixels</param>
    /// <param name="pixelFormat">the pixel format of the rows, see <see cref="WicPixelFormats"/></param>
    /// <param name="rows">supplies the rows</param>
    /// <returns>WicEncodeResult with the pixel format the encoder used and the skipped metadata</returns>
    public static WicEncodeResult Encode(Stream destination, WicEncodeOptions options, int width, int height, Guid pixelFormat, WicRowProvider rows)
    {
        if (rows == null)
        {
            throw new ArgumentNullException(nameof(rows));
        }
        ValidateEncode(destination, options, width, height);
        return Encode(destination, options, factory => new WicRowPixelSource(rows, width, height, pixelFormat, Wic.GetBitsPerPixel(factory, pixelFormat), options.DpiX, options.DpiY));
    }

    /// <summary>
    /// The options of an encoder, e.g. "ImageQuality" or "Lossless", for <see cref="WicEncodeOptions.EncoderOptions"/>
    /// </summary>
    /// <param name="containerFormat">see <see cref="WicContainerFormats"/></param>
    /// <returns>IReadOnlyList of WicEncoderOption</returns>
    /// <exception cref="WicCodecNotFoundException">when there is no encoder for the container format</exception>
    public static IReadOnlyList<WicEncoderOption> GetEncoderOptions(Guid containerFormat)
    {
        IWICImagingFactory factory = null;
        IWICBitmapEncoder encoder = null;
        IWICBitmapFrameEncode frame = null;
        IPropertyBag2 propertyBag = null;
        try
        {
            factory = WicNativeMethods.CreateImagingFactory();
            var container = containerFormat;
            Wic.Check(factory.CreateEncoder(ref container, IntPtr.Zero, out encoder), $"Creating the encoder for {containerFormat}");
            using var memoryStream = new MemoryStream();
            var stream = new ManagedIStream(memoryStream);
            Wic.Check(encoder.Initialize(stream, WicBitmapEncoderCacheOption.NoCache), "IWICBitmapEncoder.Initialize", stream);
            Wic.Check(encoder.CreateNewFrame(out frame, out propertyBag), "IWICBitmapEncoder.CreateNewFrame", stream);
            var result = new List<WicEncoderOption>();
            if (propertyBag != null)
            {
                foreach (var (name, varType) in ReadPropertyInfos(propertyBag))
                {
                    result.Add(new WicEncoderOption(name, varType));
                }
            }
            return new ReadOnlyCollection<WicEncoderOption>(result);
        }
        finally
        {
            Wic.Release(propertyBag);
            Wic.Release(frame);
            Wic.Release(encoder);
            Wic.Release(factory);
        }
    }

    private static void ValidateEncode(Stream destination, WicEncodeOptions options, int width, int height)
    {
        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The stream must be writable", nameof(destination));
        }
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be positive");
        }
        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The height must be positive");
        }
    }

    private static WicEncodeResult Encode(Stream destination, WicEncodeOptions options, Func<IWICImagingFactory, WicPixelSource> createSource)
    {
        var target = destination.CanSeek ? destination : new MemoryStream();
        IWICImagingFactory factory = null;
        IWICBitmapEncoder encoder = null;
        IWICBitmapFrameEncode frame = null;
        IPropertyBag2 propertyBag = null;
        IWICFormatConverter converter = null;
        IWICMetadataQueryWriter queryWriter = null;
        IWICPalette palette = null;
        try
        {
            factory = WicNativeMethods.CreateImagingFactory();
            var source = createSource(factory);
            var stream = new ManagedIStream(target);

            var container = options.ContainerFormat;
            Wic.Check(factory.CreateEncoder(ref container, IntPtr.Zero, out encoder), $"Creating the encoder for {options.ContainerFormat}");
            Wic.Check(encoder.Initialize(stream, WicBitmapEncoderCacheOption.NoCache), "IWICBitmapEncoder.Initialize", stream);
            Wic.Check(encoder.CreateNewFrame(out frame, out propertyBag), "IWICBitmapEncoder.CreateNewFrame", stream);

            var appliedOptions = propertyBag == null ? new List<string>() : ApplyEncoderOptions(propertyBag, options);
            Wic.Check(frame.Initialize(propertyBag), "IWICBitmapFrameEncode.Initialize", stream);
            Wic.Check(frame.SetSize((uint)source.Width, (uint)source.Height), "IWICBitmapFrameEncode.SetSize", stream);
            Wic.Check(frame.SetResolution(options.DpiX, options.DpiY), "IWICBitmapFrameEncode.SetResolution", stream);
            var pixelFormat = source.PixelFormat;
            Wic.Check(frame.SetPixelFormat(ref pixelFormat), "IWICBitmapFrameEncode.SetPixelFormat", stream);

            IReadOnlyList<WicMetadataIssue> metadataIssues = Array.Empty<WicMetadataIssue>();
            if (options.Metadata != null)
            {
                var queryWriterResult = frame.GetMetadataQueryWriter(out queryWriter);
                var metadataWriter = new WicMetadataWriter(factory, frame, queryWriterResult.Succeeded() ? queryWriter : null, options.ContainerFormat, queryWriterResult);
                try
                {
                    options.Metadata(metadataWriter);
                }
                finally
                {
                    metadataWriter.Close();
                }
                metadataIssues = metadataWriter.Issues;
            }

            IWICBitmapSource pixels = source;
            if (pixelFormat != source.PixelFormat)
            {
                // The encoder wants another pixel format, WIC converts while it pulls the rows
                Log.Debug().WriteLine("The encoder for {0} converts {1} to {2}", options.ContainerFormat, source.PixelFormat, pixelFormat);
                var indexedBits = IndexedBitsPerPixel(pixelFormat);
                if (indexedBits > 0)
                {
                    // An indexed format (e.g. GIF) needs a palette, made from the pixels, with a transparent color when the source has alpha
                    Wic.Check(factory.CreatePalette(out palette), "IWICImagingFactory.CreatePalette");
                    Wic.Check(palette.InitializeFromBitmap(source, 1u << indexedBits, HasAlpha(source.PixelFormat) ? 1 : 0), "IWICPalette.InitializeFromBitmap", source);
                    Wic.Check(frame.SetPalette(palette), "IWICBitmapFrameEncode.SetPalette", stream);
                }
                Wic.Check(factory.CreateFormatConverter(out converter), "IWICImagingFactory.CreateFormatConverter");
                Wic.Check(converter.Initialize(source, ref pixelFormat, WicBitmapDitherType.None, palette, indexedBits > 0 ? 50 : 0, WicBitmapPaletteType.Custom), "IWICFormatConverter.Initialize", source);
                pixels = (IWICBitmapSource)converter;
            }
            Wic.Check(frame.WriteSource(pixels, IntPtr.Zero), "IWICBitmapFrameEncode.WriteSource", source, stream);
            Wic.Check(frame.Commit(), "IWICBitmapFrameEncode.Commit", stream);
            Wic.Check(encoder.Commit(), "IWICBitmapEncoder.Commit", stream);
            stream.SeekToEndOfWritten();

            if (!ReferenceEquals(target, destination))
            {
                target.Position = 0;
                target.CopyTo(destination);
            }
            return new WicEncodeResult(pixelFormat, new ReadOnlyCollection<string>(appliedOptions), metadataIssues);
        }
        finally
        {
            Wic.Release(queryWriter);
            Wic.Release(palette);
            Wic.Release(converter);
            Wic.Release(propertyBag);
            Wic.Release(frame);
            Wic.Release(encoder);
            Wic.Release(factory);
            if (!ReferenceEquals(target, destination))
            {
                target.Dispose();
            }
        }
    }

    /// <summary>
    /// The bits per pixel of the indexed WIC pixel formats (GUID_WICPixelFormat1bppIndexed .. 8bppIndexed), 0 for the others
    /// </summary>
    private static int IndexedBitsPerPixel(Guid pixelFormat)
    {
        var bytes = pixelFormat.ToByteArray();
        var indexedBase = new Guid("6FDDC324-4E03-4BFE-B185-3D77768DC900").ToByteArray();
        for (int i = 0; i < 15; i++)
        {
            if (bytes[i] != indexedBase[i])
            {
                return 0;
            }
        }
        return bytes[15] switch
        {
            0x01 => 1,
            0x02 => 2,
            0x03 => 4,
            0x04 => 8,
            _ => 0
        };
    }

    private static bool HasAlpha(Guid pixelFormat) =>
        pixelFormat == WicPixelFormats.Format32bppBgra || pixelFormat == WicPixelFormats.Format32bppPbgra || pixelFormat == WicPixelFormats.Format32bppRgba ||
        pixelFormat == WicPixelFormats.Format64bppRgba || pixelFormat == WicPixelFormats.Format64bppRgbaHalf || pixelFormat == WicPixelFormats.Format128bppRgbaFloat;

    private static List<(string Name, ushort VarType)> ReadPropertyInfos(IPropertyBag2 propertyBag)
    {
        var result = new List<(string, ushort)>();
        if (!propertyBag.CountProperties(out var count).Succeeded())
        {
            return result;
        }
        for (uint i = 0; i < count; i++)
        {
            if (!propertyBag.GetPropertyInfo(i, 1, out var info, out var fetched).Succeeded() || fetched == 0)
            {
                continue;
            }
            try
            {
                var name = Marshal.PtrToStringUni(info.Name);
                if (!string.IsNullOrEmpty(name))
                {
                    result.Add((name, info.VarType));
                }
            }
            finally
            {
                if (info.Name != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(info.Name);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Set the options the encoder supports, skip the others
    /// </summary>
    private static List<string> ApplyEncoderOptions(IPropertyBag2 propertyBag, WicEncodeOptions options)
    {
        var applied = new List<string>();
        var wanted = new Dictionary<string, object>(options.EncoderOptions, StringComparer.OrdinalIgnoreCase);
        if (options.ImageQuality.HasValue)
        {
            wanted["ImageQuality"] = options.ImageQuality.Value;
        }
        if (options.Lossless.HasValue)
        {
            wanted["Lossless"] = options.Lossless.Value;
        }
        if (wanted.Count == 0)
        {
            return applied;
        }

        var supported = ReadPropertyInfos(propertyBag);
        foreach (var option in wanted)
        {
            var match = supported.FindIndex(s => string.Equals(s.Name, option.Key, StringComparison.OrdinalIgnoreCase));
            if (match < 0)
            {
                Log.Info().WriteLine("The encoder doesn't have the option {0}, skipping it", option.Key);
                continue;
            }
            var (name, varType) = supported[match];
            object value;
            try
            {
                value = ConvertToVarType(option.Value, varType);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
                Log.Warn().WriteLine("The value {0} of the encoder option {1} can't be converted to VARTYPE {2}, skipping it", option.Value, name, varType);
                continue;
            }
            var bag = new PropBag2 { Name = Marshal.StringToCoTaskMemUni(name) };
            try
            {
                var hResult = propertyBag.Write(1, ref bag, ref value);
                if (hResult.Succeeded())
                {
                    applied.Add(name);
                }
                else
                {
                    Log.Warn().WriteLine("The encoder didn't accept the option {0} = {1}: 0x{2:X8}", name, option.Value, (int)hResult);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(bag.Name);
            }
        }
        return applied;
    }

    private static object ConvertToVarType(object value, ushort varType)
    {
        var culture = CultureInfo.InvariantCulture;
        return varType switch
        {
            2 => Convert.ToInt16(value, culture),
            3 => Convert.ToInt32(value, culture),
            4 => Convert.ToSingle(value, culture),
            5 => Convert.ToDouble(value, culture),
            11 => Convert.ToBoolean(value, culture),
            16 => Convert.ToSByte(value, culture),
            17 => Convert.ToByte(value, culture),
            18 => Convert.ToUInt16(value, culture),
            19 => Convert.ToUInt32(value, culture),
            20 => Convert.ToInt64(value, culture),
            21 => Convert.ToUInt64(value, culture),
            22 => Convert.ToInt32(value, culture),
            23 => Convert.ToUInt32(value, culture),
            _ => value
        };
    }

    #endregion

    #region Decoding

    /// <summary>
    /// Read the size, resolution, pixel format and metadata of the first frame, without decoding the pixels.
    /// A seekable stream is positioned back where it was, so <see cref="Decode(Stream, Guid, IntPtr, int, bool)"/> can follow.
    /// </summary>
    /// <param name="source">the stream with the image, from its current position</param>
    /// <param name="applyOrientation">true to report the size after applying the EXIF orientation</param>
    /// <returns>WicFrameInfo</returns>
    /// <exception cref="WicCodecNotFoundException">when no decoder recognizes the stream</exception>
    public static WicFrameInfo GetFrameInfo(Stream source, bool applyOrientation = false)
    {
        var position = source != null && source.CanSeek ? source.Position : -1;
        try
        {
            return WithFrame(source, applyOrientation, (factory, frame, info) => info);
        }
        finally
        {
            if (position >= 0)
            {
                source.Position = position;
            }
        }
    }

    /// <summary>
    /// Decode the first frame into memory the caller allocated, e.g. the locked buffer of a Bitmap of the size from <see cref="GetFrameInfo"/>
    /// </summary>
    /// <param name="source">the stream with the image, from its current position</param>
    /// <param name="targetPixelFormat">the pixel format of the memory, see <see cref="WicPixelFormats"/></param>
    /// <param name="pixels">pointer to stride * height bytes</param>
    /// <param name="stride">bytes per row of the memory</param>
    /// <param name="applyOrientation">true to apply the EXIF orientation (rotate and flip), the memory then has the size of the rotated image</param>
    /// <returns>WicFrameInfo with the size and the metadata</returns>
    /// <exception cref="WicCodecNotFoundException">when no decoder recognizes the stream</exception>
    public static WicFrameInfo Decode(Stream source, Guid targetPixelFormat, IntPtr pixels, int stride, bool applyOrientation = false)
    {
        if (pixels == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(pixels));
        }
        return Decode(source, targetPixelFormat, (WicFrameInfo info, out int bufferStride) =>
        {
            bufferStride = stride;
            return pixels;
        }, applyOrientation);
    }

    /// <summary>
    /// Decode the first frame into memory which is supplied once the size is known, e.g. the locked buffer of a new Bitmap. The stream is read once.
    /// </summary>
    /// <param name="source">the stream with the image, from its current position</param>
    /// <param name="targetPixelFormat">the pixel format of the memory, see <see cref="WicPixelFormats"/></param>
    /// <param name="bufferProvider">supplies the memory and its stride</param>
    /// <param name="applyOrientation">true to apply the EXIF orientation (rotate and flip)</param>
    /// <returns>WicFrameInfo with the size and the metadata</returns>
    /// <exception cref="WicCodecNotFoundException">when no decoder recognizes the stream</exception>
    public static WicFrameInfo Decode(Stream source, Guid targetPixelFormat, WicPixelBufferProvider bufferProvider, bool applyOrientation = false)
    {
        if (bufferProvider == null)
        {
            throw new ArgumentNullException(nameof(bufferProvider));
        }
        return WithFrame(source, applyOrientation, (factory, frame, info) =>
        {
            var bitsPerPixel = Wic.GetBitsPerPixel(factory, targetPixelFormat);
            var rowBytes = Wic.RowBytes(info.Width, bitsPerPixel);
            var pixels = bufferProvider(info, out var stride);
            if (pixels == IntPtr.Zero)
            {
                throw new InvalidOperationException("The buffer provider returned no memory");
            }
            if (stride < rowBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(stride), stride, $"The stride must be at least {rowBytes}");
            }
            var bufferSize = (long)stride * (info.Height - 1) + rowBytes;
            if (bufferSize > uint.MaxValue)
            {
                throw new NotSupportedException("The image is too large for one CopyPixels call, use the WicRowConsumer overload");
            }
            WithConvertedSource(factory, frame, targetPixelFormat, converted =>
                Wic.Check(converted.CopyPixels(IntPtr.Zero, (uint)stride, (uint)bufferSize, pixels), "IWICBitmapSource.CopyPixels"));
            return info;
        });
    }

    /// <summary>
    /// Decode the first frame and hand the rows to a consumer, in bands of rows from a pooled buffer
    /// </summary>
    /// <param name="source">the stream with the image, from its current position</param>
    /// <param name="targetPixelFormat">the pixel format of the rows, see <see cref="WicPixelFormats"/></param>
    /// <param name="rows">receives the rows</param>
    /// <param name="applyOrientation">true to apply the EXIF orientation (rotate and flip)</param>
    /// <returns>WicFrameInfo with the size and the metadata</returns>
    /// <exception cref="WicCodecNotFoundException">when no decoder recognizes the stream</exception>
    public static WicFrameInfo Decode(Stream source, Guid targetPixelFormat, WicRowConsumer rows, bool applyOrientation = false)
    {
        if (rows == null)
        {
            throw new ArgumentNullException(nameof(rows));
        }
        return WithFrame(source, applyOrientation, (factory, frame, info) =>
        {
            var rowBytes = Wic.RowBytes(info.Width, Wic.GetBitsPerPixel(factory, targetPixelFormat));
            var bandRows = Math.Max(1, Math.Min(info.Height, BandBytes / Math.Max(1, rowBytes)));
            var buffer = ArrayPool<byte>.Shared.Rent(checked(bandRows * rowBytes));
            try
            {
                WithConvertedSource(factory, frame, targetPixelFormat, converted =>
                {
                    for (int top = 0; top < info.Height; top += bandRows)
                    {
                        var count = Math.Min(bandRows, info.Height - top);
                        var rect = new WicRect(0, top, info.Width, count);
                        var rectHandle = GCHandle.Alloc(rect, GCHandleType.Pinned);
                        var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                        try
                        {
                            Wic.Check(converted.CopyPixels(rectHandle.AddrOfPinnedObject(), (uint)rowBytes, (uint)(count * rowBytes), bufferHandle.AddrOfPinnedObject()), "IWICBitmapSource.CopyPixels");
                        }
                        finally
                        {
                            bufferHandle.Free();
                            rectHandle.Free();
                        }
                        rows(top, count, new ReadOnlySpan<byte>(buffer, 0, count * rowBytes), rowBytes);
                    }
                });
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return info;
        });
    }

    /// <summary>
    /// Open the stream, get the first frame (with the orientation applied when requested) and its information
    /// </summary>
    private static T WithFrame<T>(Stream source, bool applyOrientation, Func<IWICImagingFactory, IWICBitmapSource, WicFrameInfo, T> action)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (!source.CanRead)
        {
            throw new ArgumentException("The stream must be readable", nameof(source));
        }
        var input = source;
        if (!source.CanSeek)
        {
            // Decoders seek, read a stream which can't seek into memory
            var memoryStream = new MemoryStream();
            source.CopyTo(memoryStream);
            memoryStream.Position = 0;
            input = memoryStream;
        }

        IWICImagingFactory factory = null;
        IWICBitmapDecoder decoder = null;
        IWICBitmapFrameDecode frame = null;
        IWICBitmapFlipRotator flipRotator = null;
        try
        {
            factory = WicNativeMethods.CreateImagingFactory();
            var stream = new ManagedIStream(input);
            Wic.Check(factory.CreateDecoderFromStream(stream, IntPtr.Zero, WicDecodeOptions.MetadataCacheOnDemand, out decoder), "Creating a decoder for the stream", stream);
            Wic.Check(decoder.GetContainerFormat(out var containerFormat), "IWICBitmapDecoder.GetContainerFormat", stream);
            Wic.Check(decoder.GetFrameCount(out var frameCount), "IWICBitmapDecoder.GetFrameCount", stream);
            Wic.Check(decoder.GetFrame(0, out frame), "IWICBitmapDecoder.GetFrame", stream);
            Wic.Check(frame.GetSize(out var width, out var height), "IWICBitmapFrameDecode.GetSize", stream);
            Wic.Check(frame.GetPixelFormat(out var pixelFormat), "IWICBitmapFrameDecode.GetPixelFormat", stream);
            if (!frame.GetResolution(out var dpiX, out var dpiY).Succeeded())
            {
                dpiX = dpiY = 96;
            }
            var metadata = WicMetadataReading.Read(factory, frame, containerFormat);

            IWICBitmapSource pixels = (IWICBitmapSource)frame;
            var orientationApplied = false;
            if (applyOrientation && metadata.Orientation is int orientation && orientation > 1)
            {
                Wic.Check(factory.CreateBitmapFlipRotator(out flipRotator), "IWICImagingFactory.CreateBitmapFlipRotator");
                Wic.Check(flipRotator.Initialize(pixels, OrientationToTransform(orientation)), "IWICBitmapFlipRotator.Initialize", stream);
                Wic.Check(flipRotator.GetSize(out width, out height), "IWICBitmapFlipRotator.GetSize", stream);
                if (orientation >= 5)
                {
                    (dpiX, dpiY) = (dpiY, dpiX);
                }
                pixels = (IWICBitmapSource)flipRotator;
                orientationApplied = true;
            }

            var info = new WicFrameInfo((int)width, (int)height, dpiX, dpiY, pixelFormat, containerFormat, (int)frameCount, metadata, orientationApplied);
            try
            {
                return action(factory, pixels, info);
            }
            catch (COMException ex) when (stream.LastException != null && ex is not WicCodecNotFoundException)
            {
                // The stream failed while the pixels were decoded
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(stream.LastException).Throw();
                throw;
            }
        }
        finally
        {
            Wic.Release(flipRotator);
            Wic.Release(frame);
            Wic.Release(decoder);
            Wic.Release(factory);
            if (!ReferenceEquals(input, source))
            {
                input.Dispose();
            }
        }
    }

    private static void WithConvertedSource(IWICImagingFactory factory, IWICBitmapSource source, Guid targetPixelFormat, Action<IWICBitmapSource> action)
    {
        IWICFormatConverter converter = null;
        try
        {
            Wic.Check(factory.CreateFormatConverter(out converter), "IWICImagingFactory.CreateFormatConverter");
            var target = targetPixelFormat;
            Wic.Check(converter.Initialize(source, ref target, WicBitmapDitherType.None, null, 0, WicBitmapPaletteType.Custom), $"Converting to the pixel format {targetPixelFormat}");
            action((IWICBitmapSource)converter);
        }
        finally
        {
            Wic.Release(converter);
        }
    }

    /// <summary>
    /// The transformation which shows an image with the EXIF orientation upright.
    /// WIC rotates clockwise; the combinations for 5 (transpose) and 7 (transverse) are verified with a test, see WicMetadataTests.
    /// </summary>
    internal static WicBitmapTransformOptions OrientationToTransform(int orientation) => orientation switch
    {
        2 => WicBitmapTransformOptions.FlipHorizontal,
        3 => WicBitmapTransformOptions.Rotate180,
        4 => WicBitmapTransformOptions.FlipVertical,
        5 => WicBitmapTransformOptions.Rotate270 | WicBitmapTransformOptions.FlipHorizontal,
        6 => WicBitmapTransformOptions.Rotate90,
        7 => WicBitmapTransformOptions.Rotate90 | WicBitmapTransformOptions.FlipHorizontal,
        8 => WicBitmapTransformOptions.Rotate270,
        _ => WicBitmapTransformOptions.Rotate0
    };

    #endregion
}
