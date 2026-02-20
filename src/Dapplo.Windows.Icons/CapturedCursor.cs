// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NETSTANDARD2_0
using Dapplo.Windows.Common.Structs;
using System;
using System.Drawing;

namespace Dapplo.Windows.Icons;

/// <summary>
/// Represents a captured cursor, including its color and mask layers, hot spot, and system cursor status. Provides
/// resource management for the associated bitmap layers.
/// </summary>
/// <remarks>Use this class to encapsulate all relevant data for a cursor, including custom-drawn or
/// system-defined cursors. The class implements IDisposable to ensure that resources held by the color and mask layers
/// are released properly. The IsSystemCursor property indicates whether the cursor is a system-defined cursor, which
/// may affect how the cursor is rendered or manipulated.</remarks>
public class CapturedCursor: IDisposable
{
    /// <summary>
    /// Gets or sets the color layer used for the ink.
    /// </summary>
    public Bitmap ColorLayer { get; set; }
    /// <summary>
    /// Gets or sets the mask layer used to determine cutout regions in the bitmap.
    /// </summary>
    /// <remarks>The mask layer defines which areas of the bitmap are visible or transparent when applying
    /// cutout effects. Assigning a mask layer allows for advanced rendering scenarios, such as non-rectangular
    /// transparency or selective visibility based on the mask's content.</remarks>
    public Bitmap MaskLayer { get; set; }

    /// <summary>
    /// Gets or sets the coordinates of the hotspot, which is the point of interest for operations such as rendering or
    /// interaction.
    /// </summary>
    /// <remarks>The hotspot is typically used to define a specific point within a graphical element that may
    /// be used for user interactions or visual effects. Ensure that the coordinates are within the bounds of the
    /// associated graphical element to avoid unexpected behavior.</remarks>
    public NativePoint HotSpot { get; set; }

    /// <summary>
    /// Gets or sets the size of the native element.
    /// </summary>
    /// <remarks>The size is represented as a NativeSize structure, which encapsulates the dimensions of the
    /// element. Ensure that the size is set appropriately to avoid layout issues.</remarks>
    public NativeSize Size { get; set; }

    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>Call this method when the object is no longer needed to free unmanaged resources, such as
    /// associated image layers. Failing to call Dispose may result in memory leaks, especially when working with images
    /// or other unmanaged resources.</remarks>
    public void Dispose()
    {
        ColorLayer?.Dispose();
        MaskLayer?.Dispose();
    }

    /// <summary>
    /// Creates a new CapturedCursor instance that is a copy of the current instance.
    /// </summary>
    /// <remarks>The returned CapturedCursor contains copies of the ColorLayer and MaskLayer bitmaps if they
    /// are not null. Changes to the cloned instance do not affect the original instance.</remarks>
    /// <returns>A new CapturedCursor object with the same property values as the current instance.</returns>
    public CapturedCursor Clone()
    {
        return new CapturedCursor {
            ColorLayer = ColorLayer != null ? new Bitmap(ColorLayer) : null,
            MaskLayer = MaskLayer != null ? new Bitmap(MaskLayer) : null,
            HotSpot = HotSpot,
            Size = Size
        };
    }
}
#endif