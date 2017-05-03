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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Dpi;

#endregion

namespace Dapplo.Windows.FormsExample
{
    public partial class Form1 : DpiAwareForm
    {
        private static readonly LogSource Log = new LogSource();
        protected readonly BitmapScaleHandler<string> ScaleHandler;
        public Form1()
        {
            InitializeComponent();
            ScaleHandler = BitmapScaleHandler.WithComponentResourceManager(DpiHandler, GetType(), (bitmap, dpi) => ScaleIconForDisplaying(bitmap, dpi));

            // This takes care or setting the size of the images in the context menu
            DpiHandler.OnDpiChanged.Subscribe(dpi =>
            {
                var width = DpiHandler.ScaleWithDpi(20, dpi);
                var size = new Size(width, width);
                //menuStrip1.ImageScalingSize = size;
            });


            ScaleHandler.AddTarget(somethingMenuItem, "somethingMenuItem.Image");
            ScaleHandler.AddTarget(something2MenuItem, "something2MenuItem.Image");

            EnvironmentMonitor.EnvironmentUpdateEvents.Subscribe(args =>
            {
                Log.Info().WriteLine("{0} - {1}", args.SystemParametersInfoAction, args.Area);
                MessageBox.Show(this, $"{args.SystemParametersInfoAction} - {args.Area}", "Change!");
            });
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }

        /// <summary>
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="dpi"></param>
        /// <returns></returns>
        private Bitmap ScaleIconForDisplaying(Bitmap bitmap, double dpi)
        {
            var newSize = DpiHandler.ScaleWithDpi(16, dpi);
            var result = new Bitmap(newSize, newSize, bitmap.PixelFormat);
            using (var graphics = Graphics.FromImage(result))
            {
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.DrawImage(bitmap, new Rectangle(0, 0, newSize, newSize), new Rectangle(0, 0, 16, 16), GraphicsUnit.Pixel);
            }
            return result;
        }
    }
}