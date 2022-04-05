// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;

namespace Dapplo.Windows.Example.FormsExample;

/// <summary>
/// This extends the form with extra DPI aware capabilities
/// </summary>
public partial class FormWithAttachedDpiHandler : Form //DpiAwareForm
{
    private static readonly LogSource Log = new LogSource();
    private readonly BitmapScaleHandler<string, Bitmap> _scaleHandler;
    private readonly DpiHandler FormDpiHandler;
    private readonly IDisposable _dpiChangeSubscription;

    public FormWithAttachedDpiHandler()
    {
        FormDpiHandler = this.AttachDpiHandler();
        InitializeComponent();

        var initialMenuStripSize = menuStrip1.ImageScalingSize;
        FormDpiHandler.OnDpiChanged.Subscribe(dpiChangeInfo =>
        {
            menuStrip1.ImageScalingSize = DpiCalculator.ScaleWithDpi(initialMenuStripSize, dpiChangeInfo.NewDpi);
        });

        _scaleHandler = BitmapScaleHandler.WithComponentResourceManager<Bitmap>(FormDpiHandler, GetType(), BitmapScaleHandler.SimpleBitmapScaler)
            .AddTarget(somethingMenuItem, "somethingMenuItem.Image", b=>b)
            .AddTarget(something2MenuItem, "something2MenuItem.Image", b=>b);

        // This can be used to do something with DPI changes, subscription should be disposed!
        _dpiChangeSubscription = FormDpiHandler.OnDpiChanged.Subscribe(dpi =>
        {
            Log.Info().WriteLine("New DPI: {0}", dpi);
        });

        EnvironmentMonitor.EnvironmentUpdateEvents.Subscribe(args =>
        {
            Log.Info().WriteLine("{0} - {1}", args.SystemParametersInfoAction, args.Area);
            MessageBox.Show(this, $@"{args.SystemParametersInfoAction} - {args.Area}", @"Change!");
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _dpiChangeSubscription.Dispose();
        _scaleHandler.Dispose();
        base.OnClosing(e);
    }
}