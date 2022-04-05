// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
#if !NETSTANDARD2_0
using System.Windows.Forms;
#endif

namespace Dapplo.Windows.Dpi
{
    /// <summary>
    ///     Factory for the generic BitmapScaleHandler
    /// </summary>
    public static class BitmapScaleHandler
    {
        /// <summary>
        ///     Create with your own providing logic
        /// </summary>
        /// <param name="dpiHandler">DpiHandler</param>
        /// <param name="bitmapProvider">A function which provides the requested bitmap</param>
        /// <param name="bitmapScaler">A function to provide a newly scaled bitmap</param>
        public static BitmapScaleHandler<TKey, TValue> Create<TKey, TValue>(DpiHandler dpiHandler, Func<TKey, int, TValue> bitmapProvider, Func<TValue, int, TValue> bitmapScaler = null) where TValue : IDisposable
        {
            var scaleHandler = new BitmapScaleHandler<TKey, TValue>();
            scaleHandler.Initialize(dpiHandler, bitmapProvider, bitmapScaler);
            return scaleHandler;
        }

        /// <summary>
        ///     Create a BitmapScaleHandler with a ComponentResourceManager as resource provider
        /// </summary>
        /// <param name="dpiHandler">DpiHandler</param>
        /// <param name="resourceType">Type to create the ComponentResourceManager for</param>
        /// <param name="bitmapScaler">A function to provide a newly scaled bitmap, you can return the provide bitmap if you want to keep it as is</param>
        public static BitmapScaleHandler<string, TValue> WithComponentResourceManager<TValue>(DpiHandler dpiHandler, Type resourceType, Func<TValue, int, TValue> bitmapScaler = null) where TValue : IDisposable
        {
            return Create<string, TValue>(dpiHandler, (imageName, dpi) =>
            {
                var resources = new ComponentResourceManager(resourceType);
                return (TValue) resources.GetObject(imageName);
            }, bitmapScaler);
        }

        /// <summary>
        /// A simple scaling routine
        /// </summary>
        /// <param name="bitmap">Bitmap to scale</param>
        /// <param name="dpi">uint with the dpi value to scale for</param>
        /// <returns>Bitmap</returns>
        public static Bitmap SimpleBitmapScaler(Bitmap bitmap, int dpi)
        {
            if (dpi == DpiCalculator.DefaultScreenDpi)
            {
                return bitmap;
            }

            var newSize = DpiCalculator.ScaleWithDpi(bitmap.Size, dpi);
            var result = new Bitmap(newSize.Width, newSize.Height, bitmap.PixelFormat);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, newSize.Width, newSize.Height), new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
            }
            return result;
        }
    }

    /// <summary>
    ///     This provides bitmaps scaled according to the current DPI.
    ///     If the DPI changes, it will reapply the bitmaps and dispose the old ones (if needed).
    /// </summary>
    public sealed class BitmapScaleHandler<TKey, TValue> : IDisposable where TValue : IDisposable
    {
        private readonly ReaderWriterLockSlim _imagesLock = new();
        private readonly ReaderWriterLockSlim _actionsLock = new();
        private readonly Dictionary<TKey, TValue> _images = new();
        private bool _areWeDisposing;
        private int _dpi;
        private IDisposable _dpiChangeSubscription;

        internal BitmapScaleHandler()
        {
        }

        /// <summary>
        ///     A list of actions which apply the bitmap
        /// </summary>
        private Dictionary<object, Action> ApplyActions { get; } = new Dictionary<object, Action>();

        /// <summary>
        ///     This function retrieves the bitmap
        /// </summary>
        private Func<TKey, int, TValue> BitmapProvider { get; set; }

        /// <summary>
        ///     This function scales the bitmap (if needed)
        /// </summary>
        private Func<TValue, int, TValue> BitmapScaler { get; set; }

        /// <summary>
        ///     Add an action which applies a bitmap
        /// </summary>
        /// <param name="apply">Action which assigns a bitmap</param>
        /// <param name="imageKey">key of the image</param>
        /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
        public BitmapScaleHandler<TKey, TValue> AddApplyAction(Action<TValue> apply, TKey imageKey, bool execute = false)
        {
            void ApplyAction()
            {
                apply(GetBitmap(imageKey));
            }

            try
            {
                _actionsLock.EnterWriteLock();

                ApplyActions[apply] = ApplyAction;
            }
            finally
            {
                _actionsLock.ExitWriteLock();
            }
            if (execute)
            {
                ApplyAction();
            }

            return this;
        }

#if !NETSTANDARD2_0
        /// <summary>
        ///     Add a Button as a Bitmap target
        /// </summary>
        /// <param name="button">Button</param>
        /// <param name="imageKey">key of the image</param>
        /// <param name="valueConverter">func to deliver bitmaps for buttons</param>
        /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
        public BitmapScaleHandler<TKey, TValue> AddTarget(Button button, TKey imageKey, Func<TValue, Bitmap> valueConverter, bool execute = false)
        {
            void ApplyAction()
            {
                button.Image = valueConverter(GetBitmap(imageKey));
            }
            try
            {
                _actionsLock.EnterWriteLock();
                ApplyActions[button] = ApplyAction;
            }
            finally
            {
                _actionsLock.ExitWriteLock();
            }
            if (execute)
            {
                ApplyAction();
            }
            return this;
        }

        /// <summary>
        ///     Add a ButtonToolStripItem as a Bitmap target
        /// </summary>
        /// <param name="toolStripItem">ToolStripItem</param>
        /// <param name="imageKey">key of the image</param>
        /// <param name="valueConverter"></param>
        /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
        public BitmapScaleHandler<TKey, TValue> AddTarget(ToolStripItem toolStripItem, TKey imageKey, Func<TValue, Bitmap> valueConverter, bool execute = false)
        {
            void ApplyAction()
            {
                toolStripItem.Image = valueConverter(GetBitmap(imageKey));
            }
            try
            {
                _actionsLock.EnterWriteLock();

                ApplyActions[toolStripItem] = ApplyAction;
            }
            finally
            {
                _actionsLock.ExitWriteLock();
            }
            if (execute)
            {
                ApplyAction();
            }

            return this;
        }
#endif

        /// <summary>
        ///     Dispose implementation
        /// </summary>
        public void Dispose()
        {
            _dpiChangeSubscription?.Dispose();
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Processes DPI Change information
        /// </summary>
        /// <param name="dpiChangeInfo">DpiChangeInfo with the DPI information</param>
        private void ProcessDpiChange(DpiChangeInfo dpiChangeInfo)
        {
            _imagesLock.EnterWriteLock();
            List<TValue> imagesToDispose;
            try
            {
                // Make list of current bitmaps, to dispose
                imagesToDispose = _images.Values.ToList();
                _images.Clear();
            }
            finally
            {
                _imagesLock.ExitWriteLock();
            }
            // Store the current DPI value, for creating the images
            _dpi = dpiChangeInfo.NewDpi;

            try
            {
                _actionsLock.EnterReadLock();

                // Apply new images
                foreach (var key in ApplyActions.Keys)
                {
                    ApplyActions[key]();
                }
            }
            finally
            {
                _actionsLock.ExitReadLock();
            }

            // Dispose list
            foreach (var image in imagesToDispose)
            {
                image.Dispose();
            }
        }

        /// <inheritdoc />
        ~BitmapScaleHandler()
        {
            _dpiChangeSubscription?.Dispose();
            ReleaseUnmanagedResources();
        }

        /// <summary>
        ///     Get bitmaps for displaying
        /// </summary>
        /// <param name="imageKey">string with the name</param>
        /// <returns>Bitmap</returns>
        private TValue GetBitmap(TKey imageKey)
        {
            if (_areWeDisposing)
            {
                return default;
            }

            try
            {
                _imagesLock.EnterUpgradeableReadLock();
                if (_images.TryGetValue(imageKey, out var result))
                {
                    return result;
                }
                var image = BitmapProvider(imageKey, _dpi);
                if (image == null)
                {
                    return default;
                }

                if (BitmapScaler != null)
                {
                    result = BitmapScaler.Invoke(image, _dpi);
                }
                if (result == null || Equals(image, result))
                {
                    return image;
                }
                try
                {
                    _imagesLock.EnterWriteLock();
                    _images.Add(imageKey, result);
                }
                finally
                {
                    _imagesLock.ExitWriteLock();
                }
                return result;
            }
            finally
            {
                _imagesLock.ExitUpgradeableReadLock();
            }
        }

        /// <summary>
        ///     Helper method to initialize
        /// </summary>
        /// <param name="dpiHandler">DpiHandler</param>
        /// <param name="bitmapProvider">A function which provides the requested bitmap</param>
        /// <param name="bitmapScaler">A function to provide a newly scaled bitmap</param>
        internal void Initialize(DpiHandler dpiHandler, Func<TKey, int, TValue> bitmapProvider, Func<TValue, int, TValue> bitmapScaler = null)
        {
            BitmapProvider = bitmapProvider;
            BitmapScaler = bitmapScaler;
            if (dpiHandler != null)
            {
                _dpiChangeSubscription = dpiHandler.OnDpiChanged.Subscribe(ProcessDpiChange);
            }
        }

        /// <summary>
        ///     Cleanup the images, they are no longer needed
        /// </summary>
        private void ReleaseUnmanagedResources()
        {
            _areWeDisposing = true;

            try
            {
                _actionsLock.EnterReadLock();
                // Set all bitmaps to an empty one
                foreach (var applyAction in ApplyActions.Values)
                {
                    applyAction();
                }
            }
            finally
            {
                _actionsLock.ExitReadLock();
            }

            try
            {
                _imagesLock.EnterWriteLock();
                // Dispose all
                foreach (var bitmapName in _images.Keys)
                {
                    _images[bitmapName].Dispose();
                }
                _images.Clear();
            }
            finally
            {
                _imagesLock.ExitWriteLock();
            }

            try
            {
                _actionsLock.EnterWriteLock();
                // Remove actions so there are no references anymore
                ApplyActions.Clear();
            }
            finally
            {
                _actionsLock.ExitWriteLock();
            }

        }

        /// <summary>
        ///     Remove a previously added target for being updated.
        ///     This will not update the image, or remove it right away.
        /// </summary>
        public BitmapScaleHandler<TKey, TValue> RemoveTarget(object target)
        {
            try
            {
                _actionsLock.EnterWriteLock();
                ApplyActions.Remove(target);
            }
            finally
            {
                _actionsLock.ExitWriteLock();
            }
            return this;
        }
    }
}