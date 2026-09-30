// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;

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
    ///     If the DPI changes, it will reapply the bitmaps and dispose the old ones.
    ///     The BitmapScaleHandler owns every value which the provider and the scaler return: they are cached per DPI, and disposed when the DPI changes or the handler is disposed.
    ///     When the scaler returns a new instance, the one from the provider is disposed directly. So the provider must return a new instance for every call (not a shared one).
    ///     Dispose the handler on the UI thread (e.g. when the form closes), it assigns the default value to all targets.
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

        /// <summary>
        ///     Add a target, e.g. a Button, which gets a (new) bitmap applied when the DPI changes.
        ///     The target is also the key for <see cref="RemoveTarget"/>, adding the same target again replaces the previous action.
        ///     Dapplo.Windows.Forms has extensions to add a Windows Forms Button or ToolStripItem.
        /// </summary>
        /// <param name="target">object which is the target, used as key</param>
        /// <param name="imageKey">key of the image</param>
        /// <param name="apply">Action which applies the bitmap to the target</param>
        /// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
        public BitmapScaleHandler<TKey, TValue> AddTargetAction(object target, TKey imageKey, Action<TValue> apply, bool execute = false)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }
            if (apply == null)
            {
                throw new ArgumentNullException(nameof(apply));
            }
            void ApplyAction()
            {
                apply(GetBitmap(imageKey));
            }
            try
            {
                _actionsLock.EnterWriteLock();
                ApplyActions[target] = ApplyAction;
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
        ///     Stop processing DPI changes, assign the default value to all targets, and dispose all cached bitmaps.
        ///     Call this on the UI thread.
        /// </summary>
        public void Dispose()
        {
            if (_areWeDisposing)
            {
                return;
            }
            _dpiChangeSubscription?.Dispose();
            _dpiChangeSubscription = null;
            ReleaseResources();
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

                result = image;
                if (BitmapScaler != null)
                {
                    var scaled = BitmapScaler.Invoke(image, _dpi);
                    if (scaled != null && !ReferenceEquals(image, scaled))
                    {
                        // The original is replaced by the scaled one, and no longer needed
                        image.Dispose();
                        result = scaled;
                    }
                }
                // Cache what is returned, so it is reused and disposed when the DPI changes
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
            BitmapProvider = bitmapProvider ?? throw new ArgumentNullException(nameof(bitmapProvider));
            BitmapScaler = bitmapScaler;
            _dpi = dpiHandler?.Dpi ?? DpiCalculator.DefaultScreenDpi;
            if (dpiHandler != null)
            {
                _dpiChangeSubscription = dpiHandler.OnDpiChanged.Subscribe(ProcessDpiChange);
            }
        }

        /// <summary>
        ///     Cleanup the images, they are no longer needed
        /// </summary>
        private void ReleaseResources()
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