// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard.
/// The streams returned by GetAsStream and TryGetAsStream contain a copy of the clipboard content (the complete clipboard memory allocation, which can be larger than the actual data).
/// This costs memory for the size of the content, but the stream can't outlive the clipboard lock and stays valid after the access token is disposed.
/// </summary>
public static class ClipboardStreamExtensions
{
    /// <summary>
    /// Set the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">StandardClipboardFormats with the format to set the content for</param>
    /// <param name="stream">MemoryStream with the content</param>
    /// <param name="size">optional long with the number of bytes to place, needed if the stream is not seekable (otherwise it's buffered), default for a seekable stream is the remaining length</param>
    public static void SetAsStream(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format, Stream stream, long? size = null)
    {
        clipboardAccessToken.SetAsStream((uint)format, stream, size);
    }

    /// <summary>
    /// Set the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">string with the format to set the content for</param>
    /// <param name="stream">MemoryStream with the content</param>
    /// <param name="size">optional long with the number of bytes to place, needed if the stream is not seekable (otherwise it's buffered), default for a seekable stream is the remaining length</param>
    public static void SetAsStream(this IClipboardAccessToken clipboardAccessToken, string format, Stream stream, long? size = null)
    {
        clipboardAccessToken.SetAsStream(ClipboardFormatExtensions.MapFormatToId(format), stream, size);
    }

    /// <summary>
    /// Set the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="formatId">uint with the format to set the content for</param>
    /// <param name="stream">MemoryStream with the content</param>
    /// <param name="size">optional long with the number of bytes to place, needed if the stream is not seekable (otherwise it's buffered), default for a seekable stream is the remaining length</param>
    public static void SetAsStream(this IClipboardAccessToken clipboardAccessToken, uint formatId, Stream stream, long? size = null)
    {
        clipboardAccessToken.ThrowWhenNoAccess();

        if (!stream.CanRead)
        {
            throw new NotSupportedException("Can't read stream");
        }

        // The following decides how to calculate the size
        bool needsDispose = false;
        long length;
        if (stream.CanSeek)
        {
            // Calculate the rest left
            var remaining = stream.Length - stream.Position;
            if (size.HasValue && (size.Value < 0 || size.Value > remaining))
            {
                throw new ArgumentOutOfRangeException(nameof(size), $"The size {size.Value} must be between 0 and the remaining length of the stream {remaining}.");
            }
            length = size ?? remaining;
        }
        else if (size.HasValue)
        {
            length = size.Value;
        }
        else
        {
            var bufferStream = new MemoryStream();
            needsDispose = true;
            stream.CopyTo(bufferStream);
            bufferStream.Position = 0;
            length = bufferStream.Length;
            stream = bufferStream;
        }

        if (length <= 0)
        {
            throw new NotSupportedException($"Cannot write {length} length stream.");
        }

        try
        {
            // Now "paste", only when the complete payload was written the content is placed on the clipboard
            unsafe
            {
                using var writeInfo = clipboardAccessToken.WriteInfo(formatId, length);
                using (var unsafeMemoryStream = new UnmanagedMemoryStream((byte*)writeInfo.MemoryPtr, length, length, FileAccess.Write))
                {
                    CopyExactly(stream, unsafeMemoryStream, length);
                }
                writeInfo.Commit();
            }
        }
        finally
        {
            if (needsDispose)
            {
                stream.Dispose();
            }
        }
    }

    /// <summary>
    /// Copy exactly the specified number of bytes from the source to the target
    /// </summary>
    /// <param name="source">Stream to read from</param>
    /// <param name="target">Stream to write to</param>
    /// <param name="length">long with the number of bytes to copy</param>
    /// <exception cref="EndOfStreamException">When the source has less bytes than specified</exception>
    private static void CopyExactly(Stream source, Stream target, long length)
    {
        var buffer = new byte[(int)Math.Min(81920, length)];
        var remaining = length;
        while (remaining > 0)
        {
            var read = source.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
            if (read <= 0)
            {
                throw new EndOfStreamException($"The stream ended after {length - remaining} bytes, but {length} bytes were expected.");
            }
            target.Write(buffer, 0, read);
            remaining -= read;
        }
    }

    /// <summary>
    /// Try to retrieve the content for the specified format as a stream.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">StandardClipboardFormats with the format to retrieve the content for</param>
    /// <param name="stream">Stream output parameter, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</param>
    /// <returns>true if the format can be read as a stream, false otherwise</returns>
    public static bool TryGetAsStream(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format, out Stream stream)
    {
        return clipboardAccessToken.TryGetAsStream((uint)format, out stream);
    }

    /// <summary>
    /// Try to retrieve the content for the specified format as a stream.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">string with the format to retrieve the content for</param>
    /// <param name="stream">Stream output parameter, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</param>
    /// <returns>true if the format can be read as a stream, false otherwise</returns>
    public static bool TryGetAsStream(this IClipboardAccessToken clipboardAccessToken, string format, out Stream stream)
    {
        return clipboardAccessToken.TryGetAsStream(ClipboardFormatExtensions.MapFormatToId(format), out stream);
    }

    /// <summary>
    /// Try to retrieve the content for the specified format as a stream.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="formatId">uint with the format to retrieve the content for</param>
    /// <param name="stream">Stream output parameter, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</param>
    /// <returns>true if the format can be read as a stream, false otherwise</returns>
    public static bool TryGetAsStream(this IClipboardAccessToken clipboardAccessToken, uint formatId, out Stream stream)
    {
        stream = null;
        
        if (!clipboardAccessToken.TryReadInfo(formatId, out var readInfo))
        {
            return false;
        }
        
        using (readInfo)
        {
            stream = CreateReadOnlyStream(ClipboardByteExtensions.ReadBytes(readInfo));
        }
        return true;
    }

    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">StandardClipboardFormats with the format to retrieve the content for</param>
    /// <returns>Stream, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</returns>
    public static Stream GetAsStream(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format)
    {
        return clipboardAccessToken.GetAsStream((uint)format);
    }

    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">string with the format to retrieve the content for</param>
    /// <returns>Stream, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</returns>
    public static Stream GetAsStream(this IClipboardAccessToken clipboardAccessToken, string format)
    {
        return clipboardAccessToken.GetAsStream(ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="formatId">uint with the format to retrieve the content for</param>
    /// <returns>Stream, a read-only MemoryStream with a copy of the clipboard content, it stays valid after the access token is disposed</returns>
    public static Stream GetAsStream(this IClipboardAccessToken clipboardAccessToken, uint formatId)
    {
        using var readInfo = clipboardAccessToken.ReadInfo(formatId);
        return CreateReadOnlyStream(ClipboardByteExtensions.ReadBytes(readInfo));
    }

    /// <summary>
    /// Wrap the copied clipboard content in a read-only MemoryStream
    /// </summary>
    /// <param name="bytes">byte array</param>
    /// <returns>MemoryStream</returns>
    private static MemoryStream CreateReadOnlyStream(byte[] bytes)
    {
        return new MemoryStream(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);
    }
}