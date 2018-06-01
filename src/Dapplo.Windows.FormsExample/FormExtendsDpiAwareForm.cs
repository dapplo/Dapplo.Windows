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
        protected readonly BitmapScaleHandler<string> ScaleHandler;
        private readonly DpiHandler _contextMenuDpiHandler;

        public FormExtendsDpiAwareForm()
        {
            InitializeComponent();

            _contextMenuDpiHandler = contextMenuStrip1.AttachDpiHandler();

            _contextMenuDpiHandler.OnDpiChanged.Subscribe(dpi =>
            {
                Log.Info().WriteLine("ContextMenuStrip DPI: {0}", dpi);
            });
            ScaleHandler = BitmapScaleHandler.WithComponentResourceManager(FormDpiHandler, GetType(), ScaleIconForDisplaying);

            ScaleHandler.AddTarget(somethingMenuItem, "somethingMenuItem.Image");
            ScaleHandler.AddTarget(something2MenuItem, "something2MenuItem.Image");

            EnvironmentMonitor.EnvironmentUpdateEvents.Subscribe(args =>
            {
                Log.Info().WriteLine("{0} - {1}", args.SystemParametersInfoAction, args.Area);
                MessageBox.Show(this, $"{args.SystemParametersInfoAction} - {args.Area}", "Change!");
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="bitmap">Bitmap</param>
        /// <param name="dpi">double</param>
        /// <returns>Bitmap</returns>
        private Bitmap ScaleIconForDisplaying(Bitmap bitmap, double dpi)
        {
            var newSize = FormDpiHandler.ScaleWithCurrentDpi(16);
            var result = new Bitmap(newSize, newSize, bitmap.PixelFormat);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, newSize, newSize), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            return result;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            contextMenuStrip1.Show();
        }
    }
}