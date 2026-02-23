// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Dapplo.Log;
using Dapplo.Windows.AppRestartManager;
using Dapplo.Windows.AppRestartManager.Enums;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;
using Dapplo.Windows.Messages.Enumerations;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Dapplo.Windows.Example.FormsExample;

public partial class FormExtendsDpiAwareForm : DpiAwareForm
{
    private static readonly LogSource Log = new LogSource();
    private readonly BitmapScaleHandler<string, Bitmap> _scaleHandler;
    private readonly DpiHandler _contextMenuDpiHandler;
    private readonly IDisposable _dpiChangeSubscription;

    public FormExtendsDpiAwareForm()
    {
        // Handle restart manager messages to gracefully exit the application when the session is ending
        var subscription = ApplicationRestartManager.ListenForEndSession().Subscribe(endSessionMessage => {
            switch(endSessionMessage.Msg)
            {
                case WindowsMessages.WM_ENDSESSION:
                    switch (endSessionMessage.EndSessionReason)
                    {
                        case EndSessionReasons.ENDSESSION_CLOSEAPP:
                            // Application is being closed for an update
                            Application.Exit();
                            break;
                        case EndSessionReasons.ENDSESSION_LOGOFF:
                            // User is logging off
                            Application.Exit();
                            break;
                        case EndSessionReasons.ENDSESSION_CRITICAL:
                            // Critical shutdown
                            Application.Exit();
                            break;
                    }
                    break;
                case WindowsMessages.WM_QUERYENDSESSION:
                    // Return true to indicate that we can handle the session end, or false to cancel it
                    endSessionMessage.Handled = true;
                    break;
            }
        });

        InitializeComponent();

        _contextMenuDpiHandler = contextMenuStrip1.AttachDpiHandler();

        _dpiChangeSubscription = _contextMenuDpiHandler.OnDpiChanged.Subscribe(dpi =>
        {
            Log.Info().WriteLine("ContextMenuStrip DPI: {0}", dpi.NewDpi);
        });

        // TODO: Create a "SizeScaleHandler" or something
        var initialMenuStripSize = menuStrip1.ImageScalingSize;
        FormDpiHandler.OnDpiChanged.Subscribe(dpiChangeInfo =>
        {
            menuStrip1.ImageScalingSize = DpiCalculator.ScaleWithDpi(initialMenuStripSize, dpiChangeInfo.NewDpi);
        });

        _scaleHandler = BitmapScaleHandler.WithComponentResourceManager<Bitmap>(FormDpiHandler, GetType(), BitmapScaleHandler.SimpleBitmapScaler)
            .AddTarget(somethingMenuItem, "somethingMenuItem.Image", b => b)
            .AddTarget(something2MenuItem, "something2MenuItem.Image", b => b);

        EnvironmentMonitor.EnvironmentUpdateEvents.Subscribe(args =>
        {
            Log.Info().WriteLine("{0} - {1}", args.SystemParametersInfoAction, args.Area);
            MessageBox.Show(this, $@"{args.SystemParametersInfoAction} - {args.Area}", @"Change!");
        });
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _contextMenuDpiHandler.Dispose();
        _dpiChangeSubscription.Dispose();
        _scaleHandler.Dispose();
        base.OnClosing(e);
    }

    private void Button1_Click(object sender, EventArgs e)
    {
        contextMenuStrip1.Show();
    }
}