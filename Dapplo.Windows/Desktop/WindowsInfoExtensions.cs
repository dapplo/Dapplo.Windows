using System;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Native;
using System.Windows;

namespace Dapplo.Windows.Desktop
{
	/// <summary>
	/// 
	/// </summary>
	public static class WindowsInfoExtensions
	{
		/// <summary>
		/// Fill the information of the WindowInfo
		/// </summary>
		/// <param name="windowInfo">WindowInfo</param>
		public static WindowInfo Fill(this WindowInfo windowInfo)
		{
			windowInfo.Text = User32.GetText(windowInfo.Handle);
			windowInfo.Classname = User32.GetClassname(windowInfo.Handle);
			Rect rectangle;
			User32.GetClientRect(windowInfo.Handle, out rectangle);
			windowInfo.Bounds = rectangle;
			return windowInfo;
		}
		/// <summary>
		/// Get the Extended WindowStyle
		/// </summary>
		public static ExtendedWindowStyleFlags GetExtendedWindowStyle(this WindowInfo windowInfo)
		{
			return (ExtendedWindowStyleFlags)User32.GetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_EXSTYLE);
		}

		/// <summary>
		/// Set the Extended WindowStyle
		/// </summary>
		public static void SetExtendedWindowStyle(this WindowInfo windowInfo, ExtendedWindowStyleFlags value)
		{
			User32.SetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_EXSTYLE, new IntPtr((uint)value));
		}

		/// <summary>
		/// Get the WindowStyle
		/// </summary>
		public static WindowStyleFlags GetWindowStyle(this WindowInfo windowInfo)
		{
			return (WindowStyleFlags)User32.GetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_STYLE);
		}

		/// <summary>
		/// Set the WindowStyle
		/// </summary>
		public static void SetWindowStyle(this WindowInfo windowInfo, WindowStyleFlags value)
		{
			User32.SetWindowLongWrapper(windowInfo.Handle, WindowLongIndex.GWL_STYLE, new IntPtr((uint)value));
		}
	}
}
