using Dapplo.Windows.Dpi;
using System;
using System.Drawing;
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
				var width = DpiHandler.ScaleWithDpi(16, dpi);
				var size = new Size(width, width);
				menuStrip1.ImageScalingSize = size;
			});

			ScaleHandler.AddTarget(somethingMenuItem, "somethingMenuItem.Image");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bitmap"></param>
		/// <param name="dpi"></param>
		/// <returns></returns>
		private Bitmap ScaleIconForDisplaying(Bitmap bitmap, double dpi)
		{
			return bitmap;
		}
	}
}
