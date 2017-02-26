//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2017 Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

#endregion

namespace Dapplo.Windows.Dpi
{
	/// <summary>
	///     This provides bitmaps scaled according to the current DPI.
	///     If the DPI changes, it will reapply the bitmaps and dispose the old ones (if needed).
	/// </summary>
	public class BitmapScaleHandler
	{
		private static readonly Bitmap Empty = null;
		private readonly IDictionary<string, Bitmap> _images = new Dictionary<string, Bitmap>();
		private bool _areWeDisposing;
		private readonly IDisposable _dpiChangeSubscription;
		private double _dpi;

		/// <summary>
		///     Create for a ComponentResourceManager
		/// </summary>
		/// <param name="dpiHandler">DpiHandler</param>
		/// <param name="resourceType">Type to create the ComponentResourceManager for</param>
		/// <param name="bitmapScaler">A function to provide a newly scaled bitmap</param>
		public BitmapScaleHandler(DpiHandler dpiHandler, Type resourceType, Func<Bitmap, double, Bitmap> bitmapScaler = null)
		{
			var resources = new ComponentResourceManager(resourceType);
			BitmapProvider = (imageName, dpi) => (Bitmap) resources.GetObject(imageName);
			BitmapScaler = bitmapScaler;
			_dpiChangeSubscription = dpiHandler.OnDpiChanged.Subscribe(DpiChange);
		}

		/// <summary>
		///     A list of actions which apply the bitmap
		/// </summary>
		private IDictionary<object, Action> ApplyActions { get; } = new Dictionary<object, Action>();

		/// <summary>
		///     This function retrieves the bitmap
		/// </summary>
		private Func<string, double, Bitmap> BitmapProvider { get; }

		/// <summary>
		///     This function scales the bitmap (if needed)
		/// </summary>
		private Func<Bitmap, double, Bitmap> BitmapScaler { get; }

		/// <summary>
		///     Add an action which applies a bitmap
		/// </summary>
		/// <param name="apply">Action which assigns a bitmap</param>
		/// <param name="imageName">name of the image</param>
		/// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
		public void AddApplyAction(Action<Bitmap> apply, string imageName, bool execute = false)
		{
			ApplyActions[apply]  = () => apply(GetBitmap(imageName));
			if (execute)
			{
				ApplyActions[apply]();
			}
		}

		/// <summary>
		///     Add a Button as a Bitmap target
		/// </summary>
		/// <param name="button">Button</param>
		/// <param name="imageName">name of the image</param>
		/// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
		public void AddTarget(Button button, string imageName, bool execute = false)
		{
			ApplyActions[button] = () => button.Image = GetBitmap(imageName);
			if (execute)
			{
				ApplyActions[button]();
			}
		}

		/// <summary>
		///     Add a ButtonToolStripItem as a Bitmap target
		/// </summary>
		/// <param name="toolStripItem">ToolStripItem</param>
		/// <param name="imageName">name of the image</param>
		/// <param name="execute">Execute specifies if the assignment needs to be done right away</param>
		public void AddTarget(ToolStripItem toolStripItem, string imageName, bool execute = false)
		{
			ApplyActions[toolStripItem] = () => toolStripItem.Image = GetBitmap(imageName);
			if (execute)
			{
				ApplyActions[toolStripItem]();
			}
		}

		/// <summary>
		/// Remove a previously added target for being updated.
		/// This will not update the image, or remove it right away.
		/// </summary>
		public void RemoveTarget(object target)
		{
			ApplyActions.Remove(target);
		}

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
		///     Call with a new DPI setting
		/// </summary>
		/// <param name="dpi">double with the DPI value</param>
		private void DpiChange(double dpi)
		{
			// Make list of current bitmaps, to dispose
			var imagesToDispose = _images.Values.ToList();
			_images.Clear();
			// Store the current DPI value, for creating the images
			_dpi = dpi;
			// Apply new images
			foreach (var applyAction in ApplyActions.Values)
			{
				applyAction();
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
		/// <param name="imageName">string with the name</param>
		/// <returns>Bitmap</returns>
		private Bitmap GetBitmap(string imageName)
		{
			if (_areWeDisposing)
			{
				return Empty;
			}
			Bitmap result;
			if (_images.TryGetValue(imageName, out result))
			{
				return result;
			}
			var image = BitmapProvider(imageName, _dpi);
			if (image == null)
			{
				return null;
			}
			result = BitmapScaler?.Invoke(image, _dpi);
			if (result == null || Equals(image, result))
			{
				return image;
			}
			_images.Add(imageName, result);
			return result;
		}

		/// <summary>
		///     Cleanup the images, they are no longer needed
		/// </summary>
		private void ReleaseUnmanagedResources()
		{
			_areWeDisposing = true;
			// Set all bitmaps to an empty one
			foreach (var applyAction in ApplyActions.Values)
			{
				applyAction();
			}
			// Dispose all
			foreach (var bitmapName in _images.Keys.ToList())
			{
				_images[bitmapName].Dispose();
				_images.Remove(bitmapName);
			}
			// Remove actions so there are no references anymore
			ApplyActions.Clear();
		}
	}
}