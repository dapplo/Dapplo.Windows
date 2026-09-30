// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Dpi;

namespace Dapplo.Windows.Forms.Dpi;

/// <summary>
///     Extensions for the BitmapScaleHandler, to use Windows Forms controls as target
/// </summary>
public static class BitmapScaleHandlerExtensions
{
    /// <summary>
    ///     Add a Button as a Bitmap target
    /// </summary>
    /// <param name="bitmapScaleHandler">BitmapScaleHandler</param>
    /// <param name="button">Button</param>
    /// <param name="imageKey">key of the image</param>
    /// <param name="valueConverter">func to deliver bitmaps for buttons</param>
    /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
    /// <returns>BitmapScaleHandler</returns>
    public static BitmapScaleHandler<TKey, TValue> AddTarget<TKey, TValue>(this BitmapScaleHandler<TKey, TValue> bitmapScaleHandler, Button button, TKey imageKey, Func<TValue, Bitmap> valueConverter, bool execute = false) where TValue : IDisposable
    {
        if (button == null)
        {
            throw new ArgumentNullException(nameof(button));
        }
        if (valueConverter == null)
        {
            throw new ArgumentNullException(nameof(valueConverter));
        }
        return bitmapScaleHandler.AddTargetAction(button, imageKey, value => button.Image = valueConverter(value), execute);
    }

    /// <summary>
    ///     Add a ToolStripItem as a Bitmap target
    /// </summary>
    /// <param name="bitmapScaleHandler">BitmapScaleHandler</param>
    /// <param name="toolStripItem">ToolStripItem</param>
    /// <param name="imageKey">key of the image</param>
    /// <param name="valueConverter">func to deliver bitmaps for the ToolStripItem</param>
    /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
    /// <returns>BitmapScaleHandler</returns>
    public static BitmapScaleHandler<TKey, TValue> AddTarget<TKey, TValue>(this BitmapScaleHandler<TKey, TValue> bitmapScaleHandler, ToolStripItem toolStripItem, TKey imageKey, Func<TValue, Bitmap> valueConverter, bool execute = false) where TValue : IDisposable
    {
        if (toolStripItem == null)
        {
            throw new ArgumentNullException(nameof(toolStripItem));
        }
        if (valueConverter == null)
        {
            throw new ArgumentNullException(nameof(valueConverter));
        }
        return bitmapScaleHandler.AddTargetAction(toolStripItem, imageKey, value => toolStripItem.Image = valueConverter(value), execute);
    }
}
