using Dapplo.Windows.Dpi;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Dapplo.Windows.FormsExample
{
	public partial class Form1 : Form
	{
		protected readonly DpiHandler FormDpiHandler;
		protected readonly BitmapScaleHandler<string> ScaleHandler;
		public Form1()
		{
			InitializeComponent();
			// Add the Dapplo.Windows DPI change handler
			FormDpiHandler = this.HandleDpiChanges();
			ScaleHandler = BitmapScaleHandler.WithComponentResourceManager(FormDpiHandler, GetType(), (bitmap, dpi) => (Bitmap)ScaleIconForDisplaying(bitmap, dpi));

			// This takes care or setting the size of the images in the context menu
			FormDpiHandler.OnDpiChanged.Subscribe(dpi =>
			{
				var width = DpiHandler.ScaleWithDpi(20, dpi);
				var size = new Size(width, width);
				//menuStrip1.ImageScalingSize = size;
			});


			ScaleHandler.AddTarget(somethingMenuItem, "somethingMenuItem.Image");
			ScaleHandler.AddTarget(something2MenuItem, "something2MenuItem.Image");
		}

		/// <summary>
		/// 
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
				graphics.DrawImage(bitmap, new Rectangle(0,0, newSize, newSize), new Rectangle(0,0,16,16), GraphicsUnit.Pixel);
			}
			return result;
		}
	}
}
