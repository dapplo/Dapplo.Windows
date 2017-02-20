using System;
using System.Collections.Generic;
using System.Text;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Structs;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// This is the interface of all classes which represent a native window
	/// </summary>
	public interface IInteropWindow
	{
		/// <summary>
		/// Handle (ID) of the window
		/// </summary>
		IntPtr Handle { get; }

		/// <summary>
		/// Returns the bounds of this window
		/// </summary>
		RECT? Bounds { get; set; }

		/// <summary>
		/// Returns the client bounds of this window
		/// </summary>
		RECT? ClientBounds { get; set; }

		/// <summary>
		/// Returns the children of this window
		/// </summary>
		IEnumerable<IInteropWindow> Children { get; set; }

		/// <summary>
		/// Test if there are any children
		/// </summary>
		bool HasChildren { get; }

		/// <summary>
		/// string with the name of the internal class for the window
		/// </summary>
		string Classname { get; set; }

		/// <summary>
		/// Does the window have a classname?
		/// </summary>
		bool HasClassname { get; }

		/// <summary>
		/// Does this window have parent?
		/// </summary>
		bool HasParent { get; }

		/// <summary>
		/// The parent window to which this window belongs
		/// </summary>
		IntPtr? Parent { get; set; }

		/// <summary>
		/// Return the title of the window, if any
		/// </summary>
		string Caption { get; set; }

		/// <summary>
		/// Return the text (not title) of the window, if any
		/// </summary>
		string Text { get; set; }

		/// <summary>
		/// Returns true if the window is visible
		/// </summary>
		bool? IsVisible { get; set; }

		/// <summary>
		/// Returns true if the window is minimized
		/// </summary>
		bool? IsMinimized { get; set; }

		/// <summary>
		/// Returns true if the window is maximized
		/// </summary>
		bool? IsMaximized { get; set; }

		/// <summary>
		/// Get the process ID this window belongs to
		/// </summary>
		int? ProcessId { get; set; }

		/// <summary>
		/// ExtendedWindowStyleFlags for the Window
		/// </summary>
		ExtendedWindowStyleFlags? ExtendedStyle { get; set; }

		/// <summary>
		/// WindowStyleFlags for the Window
		/// </summary>
		WindowStyleFlags? Style { get; set; }

		/// <summary>
		/// WindowPlacement for the Window
		/// </summary>
		WindowPlacement? Placement { get; set; }

		/// <summary>
		/// Specifies if a WindowScroller can work with this window
		/// </summary>
		bool? CanScroll { get; set; }

		/// <summary>
		/// Dump the information in the InteropWindow for debugging
		/// </summary>
		/// <param name="cacheFlags">InteropWindowCacheFlags to specify what to dump</param>
		/// <param name="dump">StringBuilder to dump to</param>
		/// <param name="indentation">int</param>
		/// <returns>StringBuilder</returns>
		StringBuilder Dump(InteropWindowCacheFlags cacheFlags = InteropWindowCacheFlags.CacheAll, StringBuilder dump = null, string indentation = "");
	}
}