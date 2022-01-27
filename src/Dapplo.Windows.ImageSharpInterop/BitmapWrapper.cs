// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Dapplo.Windows.ImageSharpInterop
{
    /// <summary>
    /// This helps to cleanly wrap a bitmap with an Image
    /// </summary>
    public class BitmapWrapper : IDisposable
    {
        private BitmapData _bitmapData;
        private Image _imageWrapper;

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        public BitmapWrapper(Bitmap bitmap)
        {
            WrappedBitmap = bitmap;
        }

        /// <summary>
        /// This is the Bitmap which is wrapped
        /// </summary>
        public Bitmap WrappedBitmap { get;}

        /// <summary>
        /// Return the Image wrapper for the Bitmap
        /// </summary>
        /// <exception cref="NotSupportedException">when a non supported pixelformat is used</exception>
        public Image ImageWrapper {
            get
            {
                if (_imageWrapper != null)
                {
                    return _imageWrapper;
                }

                if (_bitmapData == null)
                {
                    Lock();
                }

                unsafe
                {
                    switch (_bitmapData.PixelFormat)
                    {
                        case PixelFormat.Format32bppArgb:
                            _imageWrapper = Image.WrapMemory<Bgra32>((void*)_bitmapData.Scan0, _bitmapData.Width, _bitmapData.Height);
                            break;
                        case PixelFormat.Format24bppRgb:
                            _imageWrapper = Image.WrapMemory<Bgr24>((void*)_bitmapData.Scan0, _bitmapData.Width, _bitmapData.Height);
                            break;
                        case PixelFormat.Format32bppRgb:
                            _imageWrapper = Image.WrapMemory<Bgra32>((void*)_bitmapData.Scan0, _bitmapData.Width, _bitmapData.Height);
                            break;
                        default:
                            throw new NotSupportedException($"Pixel format {_bitmapData.PixelFormat} is not supported.");
                    }
                }

                return _imageWrapper;
            }
        }

        /// <summary>
        /// Lock the bitmap, enabling that it can be wrapped
        /// </summary>
        /// <param name="workingArea">Rectangle to work on</param>
        public void Lock(Rectangle? workingArea = null)
        {
            if (_bitmapData != null)
            {
                throw new NotSupportedException("Bitmap is already locked.");
            }
            workingArea ??= new Rectangle(0, 0, WrappedBitmap.Width, WrappedBitmap.Height);
            _bitmapData = WrappedBitmap.LockBits(workingArea.Value, ImageLockMode.ReadWrite, WrappedBitmap.PixelFormat);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            WrappedBitmap.UnlockBits(_bitmapData);
            _imageWrapper = null;
        }
    }
}
