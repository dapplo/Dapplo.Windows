//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;
using Dapplo.Windows.Dpi.Forms;

#endregion

namespace Dapplo.Windows.FormsExample
{
    public partial class FormExtendsDpiAwareForm : DpiAwareForm
    {
        private static readonly LogSource Log = new LogSource();
        private readonly BitmapScaleHandler<string> _scaleHandler;
        private readonly DpiHandler _contextMenuDpiHandler;
        private readonly IDisposable _dpiChangeSubscription;

        public FormExtendsDpiAwareForm()
        {
            InitializeComponent();

            _contextMenuDpiHandler = contextMenuStrip1.AttachDpiHandler();

            _dpiChangeSubscription = _contextMenuDpiHandler.OnDpiChanged.Subscribe(dpi =>
            {
                Log.Info().WriteLine("ContextMenuStrip DPI: {0}", dpi);
            });
            _scaleHandler = BitmapScaleHandler.WithComponentResourceManager(FormDpiHandler, GetType(), ScaleIconForDisplaying)
                .AddTarget(somethingMenuItem, "somethingMenuItem.Image")
                .AddTarget(something2MenuItem, "something2MenuItem.Image");

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

        /// <summary>
        /// A simple scaling routine
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="dpi">uint</param>
        /// <returns>Bitmap</returns>
        private static Bitmap ScaleIconForDisplaying(Bitmap bitmap, uint dpi)
        {
            var newWidth = DpiHandler.ScaleWithDpi(bitmap.Width, dpi);
            var newHeight = DpiHandler.ScaleWithDpi(bitmap.Height, dpi);
            var result = new Bitmap(newWidth, newHeight, bitmap.PixelFormat);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, newWidth, newHeight), new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
            }
            return result;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show();
        }
    }
}