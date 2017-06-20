using System.Windows;
using System.Windows.Interop;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Structs;

namespace Dapplo.Windows.Extensions
{
    /// <summary>
    /// Extensions for WPF Windows
    /// </summary>
    public static class WindowsExtensions
    {
        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied Window
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow AsInteropWindow(this Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            return new InteropWindow(hwnd);
        }

        /// <summary>
        /// Place the window
        /// </summary>
        /// <param name="window">Window</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow ApplyPlacement(this Window window, WindowPlacement windowPlacement)
        {
            var interopWindow = window.AsInteropWindow();
            interopWindow.SetPlacement(windowPlacement);
            return interopWindow;
        }

        /// <summary>
        /// Returns the WindowPlacement 
        /// </summary>
        /// <param name="window">Window</param>
        /// <returns>WindowPlacement</returns>
        public static WindowPlacement RetrievePlacement(this Window window)
        {
            var interopWindow = window.AsInteropWindow();
            return interopWindow.GetPlacement();
        }
    }
}
