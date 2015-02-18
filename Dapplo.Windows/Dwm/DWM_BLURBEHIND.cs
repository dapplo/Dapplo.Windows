using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Dwm {
	[StructLayout(LayoutKind.Sequential)]
	public struct DWM_BLURBEHIND {
		public DWM_BB dwFlags;
		public bool fEnable;
		public IntPtr hRgnBlur;
		public bool fTransitionOnMaximized;
	}
}
