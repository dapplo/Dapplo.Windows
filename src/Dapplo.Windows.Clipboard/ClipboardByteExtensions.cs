// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard;

/// <summary>
/// These are extensions to work with the clipboard
/// </summary>
public static class ClipboardByteExtensions
{
    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">StandardClipboardFormats with the format to retrieve the content for</param>
    /// <returns>byte array</returns>
    public static byte[] GetAsBytes(this IClipboardAccessToken clipboardAccessToken, StandardClipboardFormats format)
    {
        return clipboardAccessToken.GetAsBytes((uint)format);
    }

    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="format">string with the format to retrieve the content for</param>
    /// <returns>byte array</returns>
    public static byte[] GetAsBytes(this IClipboardAccessToken clipboardAccessToken, string format)
    {
        return clipboardAccessToken.GetAsBytes(ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// Retrieve the content for the specified format.
    /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
    /// Note: the length of the result is the size of the clipboard memory allocation (GlobalSize), which can be larger than the actual data.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="formatId">uint with the format to retrieve the content for</param>
    /// <returns>byte array</returns>
    public static byte[] GetAsBytes(this IClipboardAccessToken clipboardAccessToken, uint formatId)
    {
        using var readInfo = clipboardAccessToken.ReadInfo(formatId);
        return ReadBytes(readInfo);
    }

    /// <summary>
    /// Copy the complete clipboard memory of the read info into a new byte array
    /// </summary>
    /// <param name="readInfo">ClipboardNativeInfo</param>
    /// <returns>byte array</returns>
    /// <exception cref="NotSupportedException">When the clipboard content is too large for a byte array</exception>
    internal static byte[] ReadBytes(ClipboardNativeInfo readInfo)
    {
        var size = readInfo.Size;
        if (size > int.MaxValue)
        {
            throw new NotSupportedException($"The clipboard content of format {readInfo.FormatId} has {size} bytes, this is too large for a byte array.");
        }
        var bytes = new byte[size];
        if (size > 0)
        {
            Marshal.Copy(readInfo.MemoryPtr, bytes, 0, (int)size);
        }
        return bytes;
    }

    /// <summary>
    /// Place byte[] on the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="bytes">bytes to place on the clipboard</param>
    /// <param name="format">StandardClipboardFormats with format to place the bytes under</param>
    public static void SetAsBytes(this IClipboardAccessToken clipboardAccessToken, byte[] bytes, StandardClipboardFormats format)
    {
        clipboardAccessToken.SetAsBytes(bytes, (uint)format);
    }

    /// <summary>
    /// Place byte[] on the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="bytes">bytes to place on the clipboard</param>
    /// <param name="format">string with the format to place the bytes under</param>
    public static void SetAsBytes(this IClipboardAccessToken clipboardAccessToken, byte[] bytes, string format)
    {
        clipboardAccessToken.SetAsBytes(bytes, ClipboardFormatExtensions.MapFormatToId(format));
    }

    /// <summary>
    /// Place byte[] on the clipboard, this assumes you already locked the clipboard.
    /// </summary>
    /// <param name="clipboardAccessToken">IClipboardLock</param>
    /// <param name="bytes">bytes to place on the clipboard</param>
    /// <param name="formatId">uint with the format ID to place the bytes under</param>
    public static void SetAsBytes(this IClipboardAccessToken clipboardAccessToken, byte[] bytes, uint formatId)
    {
        using var writeInfo = clipboardAccessToken.WriteInfo(formatId, bytes.Length);
        Marshal.Copy(bytes, 0, writeInfo.MemoryPtr, bytes.Length);
        writeInfo.Commit();
    }
}