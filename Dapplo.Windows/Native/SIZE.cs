using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace Dapplo.Windows.Native {
	[StructLayout(LayoutKind.Sequential), Serializable()]
	public struct SIZE {
		public int width;
		public int height;
		public SIZE(Size size)
			: this((int)size.Width, (int)size.Height) {

		}
		public SIZE(int width, int height) {
			this.width = width;
			this.height = height;
		}
		public Size ToSize() {
			return new Size(width, height);
		}
	}
}
