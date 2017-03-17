using Dapplo.Windows.Dpi;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Citrix;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.FormsExample
{
	public partial class Form1 : Form
	{
		private static readonly LogSource Log = new LogSource();
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

			if (WinFrame.IsAvailabe)
			{
				try
				{
					var clientDisplay = WinFrame.QuerySessionInformation<ClientDisplay>(InfoClasses.ClientDisplay);
					var clientName = WinFrame.QuerySessionInformation(InfoClasses.ClientName);
					MessageBox.Show($"Client {clientName} has {clientDisplay.ClientSize.Width}x{clientDisplay.ClientSize.Height} with {clientDisplay.ColorDepth} colors.",
						"Citrix detected");
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, "Citrix error");
				}
			}

			EnvironmentMonitor.EnvironmentUpdateEvents.Subscribe(args =>
			{
				Log.Info().WriteLine("{0} - {1}", args.SystemParametersInfoAction, args.Area);
				MessageBox.Show(this, $"{args.SystemParametersInfoAction} - {args.Area}", "Change!");
			});
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
