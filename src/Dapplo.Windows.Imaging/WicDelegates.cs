// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Dapplo.Windows.Imaging;

/// <summary>
/// Supplies rows of pixels for an encoder: copy the rows [top, top + count) into destination, row i starts at i * destinationStride.
/// Each row has width * bits per pixel / 8 bytes (rounded up), the bytes between rows (stride padding) don't matter.
/// The span ends after the last row, so it can be shorter than count * destinationStride.
/// </summary>
/// <param name="top">the first row</param>
/// <param name="count">the number of rows</param>
/// <param name="destination">where the rows go</param>
/// <param name="destinationStride">bytes from the start of one row to the start of the next row</param>
public delegate void WicRowProvider(int top, int count, Span<byte> destination, int destinationStride);

/// <summary>
/// Receives decoded rows of pixels: the rows [top, top + count), row i starts at i * sourceStride
/// </summary>
/// <param name="top">the first row</param>
/// <param name="count">the number of rows</param>
/// <param name="source">the pixels, only valid during the call</param>
/// <param name="sourceStride">bytes from the start of one row to the start of the next row</param>
public delegate void WicRowConsumer(int top, int count, ReadOnlySpan<byte> source, int sourceStride);

/// <summary>
/// Supplies the memory for a decoded image once its size is known, e.g. the Scan0 of a locked GDI+ Bitmap
/// </summary>
/// <param name="frameInfo">the size, resolution and metadata of the image which will be decoded</param>
/// <param name="stride">bytes per row of the memory, at least width * bits per pixel / 8</param>
/// <returns>pointer to stride * height bytes</returns>
public delegate IntPtr WicPixelBufferProvider(WicFrameInfo frameInfo, out int stride);
