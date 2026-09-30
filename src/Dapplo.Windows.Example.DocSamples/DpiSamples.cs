// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Enums;
using Dapplo.Windows.Forms.Dpi;
using Dapplo.Windows.User32;
using Dapplo.Windows.Wpf.Dpi;

namespace Dapplo.Windows.Example.DocSamples;

/// <summary>
/// Samples for doc/articles/dpi-awareness.md, wiki/DPI-Awareness.md
/// </summary>
public static class DpiSamples
{
    #region DpiAwareForm
    public class MainForm : DpiAwareForm
    {
        private readonly IDisposable _dpiSubscription;

        public MainForm()
        {
            // FormDpiHandler is created by DpiAwareForm, OnDpiChanged fires with the first DPI when the handle is created,
            // and for every change. WinForms scales the fonts and controls itself (Per Monitor V2), this is for everything else.
            _dpiSubscription = FormDpiHandler.OnDpiChanged.Subscribe(info =>
                Console.WriteLine($"DPI {info.PreviousDpi} -> {info.NewDpi}, scale factor {DpiCalculator.DpiScaleFactor(info.NewDpi)}"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dpiSubscription.Dispose();
            }
            base.Dispose(disposing);
        }
    }
    #endregion

    #region DpiUnawareForm
    // Windows scales this form as a bitmap (blurry, but always the right size), even when the process is DPI aware
    public class LegacyForm : DpiUnawareForm
    {
    }
    #endregion

    #region AttachDpiHandler
    public class SettingsForm : Form
    {
        private readonly DpiHandler _dpiHandler;

        public SettingsForm()
        {
            // Moves / resizes the form to the rectangle Windows suggests on a DPI change,
            // and publishes the DPI. It's disposed together with the form.
            _dpiHandler = this.AttachDpiHandler();
            _dpiHandler.OnDpiChanged.Subscribe(info => Console.WriteLine($"Now at {info.NewDpi} DPI"));
        }
    }
    #endregion

    #region BitmapScaling
    public class ToolbarForm : DpiAwareForm
    {
        private readonly ToolStripButton _saveButton = new ToolStripButton();
        private readonly BitmapScaleHandler<string, Bitmap> _scaleHandler;

        public ToolbarForm()
        {
            // Load the image from the form's resources, scale it for the current DPI, and apply it again on every DPI change.
            // The handler owns and disposes the bitmaps.
            _scaleHandler = BitmapScaleHandler.WithComponentResourceManager<Bitmap>(FormDpiHandler, GetType(), BitmapScaleHandler.SimpleBitmapScaler)
                .AddTarget(_saveButton, "saveButton.Image", bitmap => bitmap);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Dispose on the UI thread
            _scaleHandler.Dispose();
            base.OnClosing(e);
        }
    }
    #endregion

    public static void CustomBitmapProvider(DpiHandler dpiHandler, PictureBox pictureBox)
    {
        #region CustomBitmapProvider
        // Pick the best source image for the DPI yourself, return a new Bitmap for every call: the handler disposes them
        var scaleHandler = BitmapScaleHandler.Create<string, Bitmap>(dpiHandler,
            (name, dpi) => new Bitmap(dpi > 120 ? $"Images\\{name}@2x.png" : $"Images\\{name}.png"));

        scaleHandler.AddTargetAction(pictureBox, "logo", bitmap => pictureBox.Image = bitmap, execute: true);
        #endregion
    }

    public static void ContextMenu(ContextMenuStrip contextMenuStrip)
    {
        #region ContextMenu
        // A ContextMenuStrip is its own window, it can show up on another monitor than the form
        DpiHandler menuDpiHandler = contextMenuStrip.AttachDpiHandler();
        menuDpiHandler.OnDpiChanged.Subscribe(info => contextMenuStrip.ImageScalingSize = DpiCalculator.ScaleWithDpi(new Size(16, 16), info.NewDpi));
        #endregion
    }

    public static void Wpf(System.Windows.Window window)
    {
        #region Wpf
        // With a Per Monitor (V2) manifest WPF scales the window itself, the handler only reports the DPI.
        // Without it, a LayoutTransform is applied to the content when the monitor DPI differs from the DPI WPF renders with.
        DpiHandler dpiHandler = window.AttachDpiHandler();
        dpiHandler.OnDpiChanged.Subscribe(info => Console.WriteLine($"{window.Title} is now at {info.NewDpi} DPI"));
        #endregion
    }

    public static void Calculations()
    {
        #region Calculations
        // 96 DPI is 100%, 144 DPI is 150%
        int scaled = DpiCalculator.ScaleWithDpi(16, 144);      // 24
        int unscaled = DpiCalculator.UnscaleWithDpi(24, 144);  // 16
        float factor = DpiCalculator.DpiScaleFactor(120);     // 1.25

        // Structs are scaled too, values are rounded
        NativeSize iconSize = DpiCalculator.ScaleWithDpi(new NativeSize(32, 32), 120); // 40x40

        // From one DPI to another: 144 -> 96 is 0.667
        float factor144To96 = DpiCalculator.DpiScaleFactor(144, 96);
        #endregion
    }

    public static void HandlerScaling(DpiHandler dpiHandler)
    {
        #region HandlerScaling
        // A DpiHandler knows the DPI of its window: Dpi is 96 until the first DPI is known (IsDpiKnown)
        int margin = dpiHandler.ScaleWithCurrentDpi(8);
        NativeSize buttonSize = dpiHandler.ScaleWithCurrentDpi(new NativeSize(75, 23));
        #endregion
    }

    public static void GetDpi(IntPtr hWnd)
    {
        #region GetDpi
        // The DPI of a window, and of the monitor at a location
        int windowDpi = NativeDpiMethods.GetDpi(hWnd);
        int cursorDpi = NativeDpiMethods.GetDpi(User32Api.GetCursorLocation());

        // The DPI of every display
        foreach (var display in DisplayInfo.AllDisplayInfos)
        {
            Console.WriteLine($"{display.DeviceName} {display.Bounds}: {NativeDpiMethods.GetDpi(display.Bounds.Location)} DPI");
        }
        #endregion
    }

    public static void EnableDpiAware()
    {
        #region EnableDpiAware
        // Prefer the manifest. If that's not possible, call this before any window is created:
        // it tries Per Monitor V2, then Per Monitor, and returns if the process is DPI aware afterwards.
        if (!NativeDpiMethods.EnableDpiAware())
        {
            Console.WriteLine("The process is not DPI aware, Windows scales it as a bitmap");
        }
        #endregion
    }

    public static void AwarenessContext()
    {
        #region AwarenessContext
        // Awareness contexts are handles: compare them with AreDpiAwarenessContextsEqual, not with ==
        var threadContext = NativeDpiMethods.GetThreadDpiAwarenessContext();
        bool isPerMonitorV2 = NativeDpiMethods.AreDpiAwarenessContextsEqual(threadContext, DpiAwarenessContext.PerMonitorAwareV2);

        // Create a window with another awareness, the previous context of the thread is restored when the scope is disposed
        using (NativeDpiMethods.ScopedThreadDpiAwarenessContext(DpiAwarenessContext.SystemAware))
        {
            var toolWindow = new Form();
            toolWindow.CreateControl();
        }
        #endregion
    }
}
