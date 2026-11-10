// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Windows.Clipboard.Internals;

/// <summary>
/// Reads clipboard content as a ReadOnlySpan, without copying it into a byte array first.
/// </summary>
/// <typeparam name="T">Type of the result</typeparam>
/// <param name="data">ReadOnlySpan with the content, only valid during the call</param>
/// <returns>T</returns>
internal delegate T ClipboardSpanReader<out T>(ReadOnlySpan<byte> data);

/// <summary>
/// Read clipboard content without the intermediate copies: directly from the locked global memory of the clipboard,
/// or from the buffer of a MemoryStream. Large content (images, long texts) otherwise ends up on the large object heap, once or twice.
/// </summary>
internal static class ClipboardSpanReading
{
    /// <summary>
    /// Pass the content of the clipboard format, as it is in the global memory, to the reader
    /// </summary>
    /// <param name="readInfo">ClipboardNativeInfo</param>
    /// <param name="reader">ClipboardSpanReader</param>
    /// <returns>T</returns>
    public static unsafe T Read<T>(ClipboardNativeInfo readInfo, ClipboardSpanReader<T> reader)
    {
        var size = readInfo.Size;
        if (size > int.MaxValue)
        {
            throw new NotSupportedException($"The clipboard content of format {readInfo.FormatId} has {size} bytes, this is too large for a byte array.");
        }
        return reader(size == 0 ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>((void*)readInfo.MemoryPtr, (int)size));
    }

    /// <summary>
    /// Pass the content of the format to the reader, without copying it when the source is the clipboard itself or holds the content in memory
    /// </summary>
    /// <param name="source">IClipboardDataSource</param>
    /// <param name="format">string with the format</param>
    /// <param name="reader">ClipboardSpanReader</param>
    /// <param name="result">T with the result of the reader</param>
    /// <returns>bool true when the format was available</returns>
    public static bool TryRead<T>(IClipboardDataSource source, string format, ClipboardSpanReader<T> reader, out T result)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }
        result = default;

        if (source is ClipboardTokenDataSource tokenSource)
        {
            var token = tokenSource.Token;
            token.ThrowWhenNoAccess();
            if (string.IsNullOrEmpty(format) || !token.TryReadInfo(ClipboardFormatExtensions.MapFormatToId(format), out var readInfo))
            {
                return false;
            }
            using (readInfo)
            {
                result = Read(readInfo, reader);
            }
            return true;
        }

        if (!source.TryGetStream(format, out var stream) || stream == null)
        {
            return false;
        }
        using (stream)
        {
            if (stream is MemoryStream memoryStream)
            {
                // The complete content, independent of the position, like MemoryStream.ToArray
                result = memoryStream.TryGetBuffer(out var segment)
                    ? reader(new ReadOnlySpan<byte>(segment.Array, segment.Offset, segment.Count))
                    : reader(memoryStream.ToArray());
                return true;
            }
            using var copy = new MemoryStream();
            stream.CopyTo(copy);
            copy.TryGetBuffer(out var copied);
            result = reader(new ReadOnlySpan<byte>(copied.Array, copied.Offset, copied.Count));
        }
        return true;
    }

    /// <summary>
    /// Decode UTF-16 text which ends at the first NUL character, or at the end of the data.
    /// The memory allocation of the clipboard can be larger than the string, and it's not always zero initialized.
    /// </summary>
    /// <param name="data">ReadOnlySpan with the bytes</param>
    /// <returns>string</returns>
    public static unsafe string DecodeUnicode(ReadOnlySpan<byte> data)
    {
        var terminator = MemoryMarshal.Cast<byte, char>(data).IndexOf('\0');
        var byteCount = terminator >= 0 ? terminator * 2 : data.Length;
        if (byteCount == 0)
        {
            return string.Empty;
        }
        fixed (byte* bytes = data)
        {
            return Encoding.Unicode.GetString(bytes, byteCount);
        }
    }

    /// <summary>
    /// Decode UTF-8 text which ends at the first NUL byte, or at the end of the data.
    /// </summary>
    /// <param name="data">ReadOnlySpan with the bytes</param>
    /// <returns>string</returns>
    public static unsafe string DecodeUtf8(ReadOnlySpan<byte> data)
    {
        var terminator = data.IndexOf((byte)0);
        var byteCount = terminator >= 0 ? terminator : data.Length;
        if (byteCount == 0)
        {
            return string.Empty;
        }
        fixed (byte* bytes = data)
        {
            return Encoding.UTF8.GetString(bytes, byteCount);
        }
    }
}
