using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32.Structs;
using System.Windows.Forms;

namespace Dapplo.Windows.Extensions
{
    /// <summary>
    /// Extensions for Forms
    /// </summary>
    public static class FormsExtensions
    {
        /// <summary>
        ///     Factory method to create a InteropWindow for the supplied WindowForm
        /// </summary>
        /// <param name="form">Form</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow AsInteropWindow(this Form form)
        {
            return new InteropWindow(form.Handle);
        }

        /// <summary>
        /// Place the Form
        /// </summary>
        /// <param name="form">Form</param>
        /// <param name="windowPlacement">WindowPlacement</param>
        /// <returns>InteropWindow</returns>
        public static InteropWindow ApplyPlacement(this Form form, WindowPlacement windowPlacement)
        {
            var interopWindow = form.AsInteropWindow();
            interopWindow.SetPlacement(windowPlacement);
            return interopWindow;
        }

        /// <summary>
        /// Returns the WindowPlacement 
        /// </summary>
        /// <param name="form">WindowForm</param>
        /// <returns>WindowPlacement</returns>
        public static WindowPlacement RetrievePlacement(this Form form)
        {
            var interopWindow = form.AsInteropWindow();
            return interopWindow.GetPlacement();
        }
    }
}
