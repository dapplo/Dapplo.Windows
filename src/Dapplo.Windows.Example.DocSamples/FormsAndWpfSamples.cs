// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Forms;
using Dapplo.Windows.Icons;
using Dapplo.Windows.User32.Structs;
using Dapplo.Windows.Wpf;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/forms-and-wpf.md
/// </summary>
public static class FormsAndWpfSamples
{
    public static void FormsPlacement(Form form)
    {
        #region FormsPlacement
        // Save the placement (normal bounds, maximized / minimized) when closing ...
        WindowPlacement placement = form.RetrievePlacement();
        // ... and restore it before the form is shown the next time
        form.ApplyPlacement(placement);

        // Everything of Dapplo.Windows for your own form
        InteropWindow interopWindow = form.AsInteropWindow();
        #endregion
    }

    public static void WpfWindow(System.Windows.Window window)
    {
        #region WpfWindow
        // The handle, it's created when needed (also before the window is shown)
        IntPtr handle = window.GetHandle();
        InteropWindow interopWindow = window.AsInteropWindow();

        WindowPlacement placement = window.RetrievePlacement();
        window.ApplyPlacement(placement);
        #endregion
    }

    public static void WpfConversions(NativeRect nativeRect)
    {
        #region WpfConversions
        // Between the native structs and WPF
        System.Windows.Rect wpfRect = nativeRect.ToRect();
        System.Windows.Int32Rect int32Rect = nativeRect.ToInt32Rect();
        NativeRectFloat back = wpfRect.ToNativeRectFloat();
        NativeRect fromInt32Rect = int32Rect.ToNativeRect();

        // System.Drawing types convert implicitly
        Rectangle drawingRectangle = nativeRect;
        NativeRect fromDrawing = drawingRectangle;
        #endregion
    }

    public static void WpfImages(IInteropWindow window)
    {
        #region WpfImages
        // A screenshot of a window for an Image control
        BitmapSource screenshot = window.PrintWindowAsBitmapSource();

        // A Bitmap or Icon for WPF, the alpha channel is kept
        using (var icon = window.GetIcon<Icon>())
        {
            BitmapSource iconSource = icon?.ToBitmapSource();
        }

        // The accent color as WPF color
        System.Windows.Media.Color accent = DwmApi.ColorizationColor.ToMediaColor();
        #endregion
    }
}
