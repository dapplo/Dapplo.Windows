using System.Windows;

namespace Dapplo.Windows.Native {
	/// <summary>
	/// The struct that contains the display information
	/// </summary>
	public class DisplayInfo {
		public string DeviceName { get; set; }
		public MonitorInfoFlags Flags { get; set; }
		public Rect Bounds { get; set; }
		public Rect WorkArea { get; set; }
	}
}
