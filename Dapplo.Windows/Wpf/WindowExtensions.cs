#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Dapplo.Windows.Desktop;

#endregion

namespace Dapplo.Windows.Wpf
{
	/// <summary>
	///     Extensions for the WPF Window class
	/// </summary>
	public static class WindowExtensions
	{
		/// <summary>
		///     Handle DPI changes for the specified Window
		/// </summary>
		/// <param name="window">Window</param>
		/// <returns>DpiHandler</returns>
		public static DpiHandler HandleDpiChanges(this Window window)
		{
			var dpiHandler = new DpiHandler();
			var hwndSource = (HwndSource) PresentationSource.FromVisual(window);
			if (hwndSource == null)
			{
				throw new NotSupportedException("No HwndSource available?");
			}
			hwndSource.AddHook(dpiHandler.HandleMessages);
			dpiHandler.MessageHandler = hwndSource;
			// Add the layout transform action
			dpiHandler.OnDpiChangedAction = (dpi, scaleFactor) => window.UpdateLayoutTransform(scaleFactor);
			return dpiHandler;
		}

		/// <summary>
		///     This can be used to change the scaling of the FrameworkElement
		/// </summary>
		/// <param name="frameworkElement">FrameworkElement</param>
		/// <param name="scaleFactor">double with the factor (1.0 = 100% = 96 dpi)</param>
		public static void UpdateLayoutTransform(this FrameworkElement frameworkElement, double scaleFactor)
		{
			// Adjust the rendering graphics and text size by applying the scale transform to the top level visual node of the Window
			var child = VisualTreeHelper.GetChild(frameworkElement, 0);
			if (Math.Abs(scaleFactor - 1.0) < 3 * double.Epsilon)
			{
				var scaleTransform = new ScaleTransform(scaleFactor, scaleFactor);
				child.SetValue(FrameworkElement.LayoutTransformProperty, scaleTransform);
			}
			else
			{
				child.SetValue(FrameworkElement.LayoutTransformProperty, null);
			}
		}
	}
}