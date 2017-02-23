using System;
using System.Windows.Forms;
using System.Windows.Interop;
using Dapplo.Windows.Desktop;

namespace Dapplo.Windows.Forms
{
	/// <summary>
	/// Extensions for Windows Form
	/// </summary>
	public static class FormsExtensions
	{
		/// <summary>
		/// Handle DPI changes for the specified Form
		/// </summary>
		/// <param name="form">Form</param>
		/// <returns>DpiHandler</returns>
		public static DpiHandler HandleDpiChanges(this Form form)
		{
			var dpiHandler = new DpiHandler();
			var hwndSource = HwndSource.FromHwnd(form.Handle);
			if (hwndSource == null)
			{
				throw new NotSupportedException("No HwndSource available?");
			}
			hwndSource.AddHook(dpiHandler.HandleMessages);
			return dpiHandler;
		}
	}
}
