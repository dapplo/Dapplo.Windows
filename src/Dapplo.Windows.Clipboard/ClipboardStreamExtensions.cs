// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using Dapplo.Windows.Clipboard.Internals;

namespace Dapplo.Windows.Clipboard
{
    /// <summary>
    /// These are extensions to work with the clipboard
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
        /// <param name="size">long with the size, if the stream is not seekable</param>
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
        /// <param name="size">long with the size, if the stream is not seekable</param>
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
        /// <param name="size">long with the size, if the stream is not seekable</param>
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
                length = stream.Length - stream.Position;
                if (length <= 0)
                {
                    throw new NotSupportedException($"Cannot write {length} length stream.");
                }
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
                length = bufferStream.Length;
                stream = bufferStream;
            }

            // Now "paste"
            unsafe
            {
                using (var writeInfo = clipboardAccessToken.WriteInfo(formatId, length))
                using (var unsafeMemoryStream = new UnmanagedMemoryStream((byte*)writeInfo.MemoryPtr, length, length, FileAccess.Write))
                {
                    stream.CopyTo(unsafeMemoryStream);
                }
                if (needsDispose)
                {
                    stream.Dispose();
                }
            }
        }

        /// <summary>
        /// Retrieve the content for the specified format.
        /// You will need to "lock" (OpenClipboard) the clipboard before calling this.
        /// </summary>
        /// <param name="clipboardAccessToken">IClipboardLock</param>
        /// <param name="format">StandardClipboardFormats with the format to retrieve the content for</param>
        /// <returns>MemoryStream</returns>
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
        /// <returns>MemoryStream</returns>
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
        /// <returns>MemoryStream</returns>
        public static Stream GetAsStream(this IClipboardAccessToken clipboardAccessToken, uint formatId)
        {
            var readInfo = clipboardAccessToken.ReadInfo(formatId);
            // return the memory stream, the global unlock is done when the UnmanagedMemoryStreamWrapper is disposed
            unsafe
            {
                var result = new UnmanagedMemoryStreamWrapper((byte*)readInfo.MemoryPtr, readInfo.Size, readInfo.Size, FileAccess.Read);
                // Make sure the readinfo is disposed when needed
                result.SetDisposable(readInfo);
                return result;
            }
        }
    }
}
