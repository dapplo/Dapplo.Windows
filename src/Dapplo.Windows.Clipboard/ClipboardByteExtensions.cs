// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
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
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardLock</param>
        /// <param name="formatId">uint with the format to retrieve the content for</param>
        /// <returns>byte array</returns>
        public static byte[] GetAsBytes(this IClipboardAccessToken clipboardAccessToken, uint formatId)
        {
            using var readInfo = clipboardAccessToken.ReadInfo(formatId);
            var bytes = new byte[readInfo.Size];

            // Fill the memory stream
            Marshal.Copy(readInfo.MemoryPtr, bytes, 0, readInfo.Size);
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
            unsafe
            {
                using var unsafeMemoryStream = new UnmanagedMemoryStream((byte*)writeInfo.MemoryPtr, bytes.Length, bytes.Length, FileAccess.Write);
                unsafeMemoryStream.Write(bytes, 0, bytes.Length);
            }
        }
    }
}
