using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Native {
	/// <summary>
	/// The structure for the WindowInfo
	/// See: http://msdn.microsoft.com/en-us/library/windows/desktop/ms632610.aspx
	/// </summary>
	[StructLayout(LayoutKind.Sequential), Serializable]
	internal struct WINDOWINFO {
		public uint cbSize;
		public RECT rcWindow;
		public RECT rcClient;
		public uint dwStyle;
		public uint dwExStyle;
		public uint dwWindowStatus;
		public uint cxWindowBorders;
		public uint cyWindowBorders;
		public ushort atomWindowType;
		public ushort wCreatorVersion;
		// Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
		public WINDOWINFO(Boolean? filler)
			: this() {
			cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
		}
	}
}
