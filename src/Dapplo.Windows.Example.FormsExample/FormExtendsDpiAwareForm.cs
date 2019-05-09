//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;

#endregion

namespace Dapplo.Windows.Example.FormsExample
{
    public partial class FormExtendsDpiAwareForm : DpiAwareForm
    {
        private static readonly LogSource Log = new LogSource();
        private readonly BitmapScaleHandler<string, Bitmap> _scaleHandler;
        private readonly DpiHandler _contextMenuDpiHandler;
        private readonly IDisposable _dpiChangeSubscription;

        public FormExtendsDpiAwareForm()
        {
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
                menuStrip1.ImageScalingSize = DpiHandler.ScaleWithDpi(initialMenuStripSize, dpiChangeInfo.NewDpi);
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
}