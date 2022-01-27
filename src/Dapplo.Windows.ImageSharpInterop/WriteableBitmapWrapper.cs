// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Windows;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace Dapplo.Windows.ImageSharpInterop
{
    /// <summary>
    /// This helps to cleanly wrap a WriteableBitmap with an Image
    /// </summary>
    public class WriteableBitmapWrapper : IImageWrapper, IDisposable
    {
        private Image _imageWrapper;
        private bool _isLocked;
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="writeableBitmap">WriteableBitmap</param>
        public WriteableBitmapWrapper(WriteableBitmap writeableBitmap)
        {
            WrappedWriteableBitmap = writeableBitmap;
        }

        /// <summary>
        /// This is the WriteableBitmap which is wrapped
        /// </summary>
        public WriteableBitmap WrappedWriteableBitmap { get;}

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

                if (!_isLocked)
                {
                    Lock();
                }
                unsafe
                {
                    switch (WrappedWriteableBitmap.Format.BitsPerPixel)
                    {
                        case 32:
                            _imageWrapper = Image.WrapMemory<Bgra32>((void*)WrappedWriteableBitmap.BackBuffer, (int)WrappedWriteableBitmap.Width, (int)WrappedWriteableBitmap.Height);
                            break;
                        case 24:
                            _imageWrapper = Image.WrapMemory<Bgr24>((void*)WrappedWriteableBitmap.BackBuffer, (int)WrappedWriteableBitmap.Width, (int)WrappedWriteableBitmap.Height);
                            break;
                        default:
                            throw new NotSupportedException($"Pixel format {WrappedWriteableBitmap.Format} is not supported.");
                    }
                }

                return _imageWrapper;
            }
        }

        /// <summary>
        /// Lock the WriteableBitmap, enabling that it can be wrapped
        /// </summary>
        public void Lock()
        {
            if (_isLocked)
            {
                throw new NotSupportedException("WriteableBitmap is already locked.");
            }

            _isLocked = true;
            WrappedWriteableBitmap.Lock();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (!_isLocked)
            {
                return;
            }
            var workingArea = new Int32Rect(0, 0, (int)WrappedWriteableBitmap.Width, (int)WrappedWriteableBitmap.Height);
            WrappedWriteableBitmap.AddDirtyRect(workingArea);
            WrappedWriteableBitmap.Unlock();
        }
    }
}
