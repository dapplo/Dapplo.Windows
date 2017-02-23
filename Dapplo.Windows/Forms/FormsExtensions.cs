using System.Windows.Forms;
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
			var listener = new WinProcListener(form);
			listener.AddHook(dpiHandler.HandleMessages);
			dpiHandler.MessageHandler = listener;
			return dpiHandler;
		}
	}
}
