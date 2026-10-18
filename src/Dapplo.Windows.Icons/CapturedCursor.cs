// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Windows.Common.Structs;
using System;
using System.Drawing;
using System.Drawing.Imaging;

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
    /// <remarks>The hotspot is relative to the top-left of the layers (in the same scale as <see cref="Size"/>),
    /// subtract it from the mouse position to get the position to draw the cursor at.
    /// After <see cref="Trim"/> it can be outside of the layers, even negative, when it sits in a transparent part of the cursor
    /// (e.g. the hot spot of a cursor whose arrow doesn't start at its tip): drawing at "mouse position minus HotSpot" still puts every pixel where it was.</remarks>
    public NativePoint HotSpot { get; set; }

    /// <summary>
    /// Gets or sets the size of the cursor, for a captured cursor this is the size of the ColorLayer and MaskLayer.
    /// </summary>
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

    /// <summary>
    /// Creates a new CapturedCursor without the transparent border: the layers are cropped to the smallest rectangle which holds every visible pixel.
    /// The cursor canvas (32x32, 48x48 at 150 %, up to 256x256 with a bigger mouse pointer) is mostly empty, the visible arrow only fills a corner of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pixel is visible in an alpha cursor (no <see cref="MaskLayer"/>) when its alpha is more than 0. In a mask cursor a pixel is transparent only
    /// when the mask is white (AND = 1) and the color is black (XOR = 0), it leaves the screen as it is; every other pixel is visible, also the
    /// pixels which invert the screen (mask white, color not black), e.g. of the I-beam.
    /// </para>
    /// <para>
    /// The layers keep their pixel format, <see cref="Size"/> is the size of the visible bounds and <see cref="HotSpot"/> is moved by their top-left,
    /// so it can end up outside of the new layers, even negative (see <see cref="HotSpot"/>). The bounds are not grown to hold the hot spot:
    /// drawing at "mouse position minus HotSpot" puts every pixel where it was.
    /// When nothing at the border is transparent, or nothing at all is visible, the result is a plain <see cref="Clone"/>.
    /// The caller still owns this instance, dispose both.
    /// </para>
    /// </remarks>
    /// <returns>A new CapturedCursor, the caller disposes it</returns>
    public CapturedCursor Trim()
    {
        if (ColorLayer == null || (MaskLayer != null && MaskLayer.Size != ColorLayer.Size))
        {
            return Clone();
        }
        var bounds = MaskLayer == null ? GetAlphaBounds(ColorLayer) : GetMaskedBounds(ColorLayer, MaskLayer);
        var fullBounds = new Rectangle(0, 0, ColorLayer.Width, ColorLayer.Height);
        if (bounds.IsEmpty || bounds == fullBounds)
        {
            return Clone();
        }

        // The hot spot and the size are in the scale of Size, which is the layer size for a captured cursor
        var size = Size.IsEmpty ? new NativeSize(ColorLayer.Width, ColorLayer.Height) : Size;
        int ScaleX(int value) => size.Width == ColorLayer.Width ? value : (int)Math.Round(value * (double)size.Width / ColorLayer.Width);
        int ScaleY(int value) => size.Height == ColorLayer.Height ? value : (int)Math.Round(value * (double)size.Height / ColorLayer.Height);

        Bitmap colorLayer = null;
        Bitmap maskLayer = null;
        try
        {
            colorLayer = ColorLayer.Clone(bounds, ColorLayer.PixelFormat);
            maskLayer = MaskLayer?.Clone(bounds, MaskLayer.PixelFormat);
            var trimmed = new CapturedCursor
            {
                ColorLayer = colorLayer,
                MaskLayer = maskLayer,
                HotSpot = new NativePoint(HotSpot.X - ScaleX(bounds.X), HotSpot.Y - ScaleY(bounds.Y)),
                Size = new NativeSize(ScaleX(bounds.Right) - ScaleX(bounds.X), ScaleY(bounds.Bottom) - ScaleY(bounds.Y))
            };
            colorLayer = null;
            maskLayer = null;
            return trimmed;
        }
        finally
        {
            colorLayer?.Dispose();
            maskLayer?.Dispose();
        }
    }

    /// <summary>
    /// The bounds of the pixels with an alpha above 0, empty when there are none
    /// </summary>
    private static unsafe Rectangle GetAlphaBounds(Bitmap colorLayer)
    {
        var bounds = new BoundsBuilder();
        var data = colorLayer.LockBits(new Rectangle(0, 0, colorLayer.Width, colorLayer.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < data.Height; y++)
            {
                var row = (byte*)data.Scan0 + y * data.Stride;
                for (var x = 0; x < data.Width; x++)
                {
                    if (row[x * 4 + 3] != 0)
                    {
                        bounds.Add(x, y);
                    }
                }
            }
        }
        finally
        {
            colorLayer.UnlockBits(data);
        }
        return bounds.ToRectangle();
    }

    /// <summary>
    /// The bounds of the pixels which change the screen: transparent is only mask white (AND = 1) with color black (XOR = 0), empty when there are none
    /// </summary>
    private static unsafe Rectangle GetMaskedBounds(Bitmap colorLayer, Bitmap maskLayer)
    {
        var width = colorLayer.Width;
        var height = colorLayer.Height;
        var area = new Rectangle(0, 0, width, height);
        // One pass over the mask: which pixels are white
        var maskWhite = new bool[width * height];
        var maskData = maskLayer.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < height; y++)
            {
                var row = (uint*)((byte*)maskData.Scan0 + y * maskData.Stride);
                for (var x = 0; x < width; x++)
                {
                    maskWhite[y * width + x] = (row[x] & 0x00FFFFFF) == 0x00FFFFFF;
                }
            }
        }
        finally
        {
            maskLayer.UnlockBits(maskData);
        }

        // One pass over the color layer
        var bounds = new BoundsBuilder();
        var colorData = colorLayer.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (var y = 0; y < height; y++)
            {
                var row = (uint*)((byte*)colorData.Scan0 + y * colorData.Stride);
                for (var x = 0; x < width; x++)
                {
                    var colorBlack = (row[x] & 0x00FFFFFF) == 0;
                    if (!(maskWhite[y * width + x] && colorBlack))
                    {
                        bounds.Add(x, y);
                    }
                }
            }
        }
        finally
        {
            colorLayer.UnlockBits(colorData);
        }
        return bounds.ToRectangle();
    }

    /// <summary>
    /// Collects the smallest rectangle holding all added pixels
    /// </summary>
    private struct BoundsBuilder
    {
        private int _left, _top, _right, _bottom;
        private bool _any;

        public void Add(int x, int y)
        {
            if (!_any)
            {
                _left = _right = x;
                _top = _bottom = y;
                _any = true;
                return;
            }
            if (x < _left) _left = x;
            if (x > _right) _right = x;
            if (y < _top) _top = y;
            if (y > _bottom) _bottom = y;
        }

        public Rectangle ToRectangle() => _any ? Rectangle.FromLTRB(_left, _top, _right + 1, _bottom + 1) : Rectangle.Empty;
    }
}
