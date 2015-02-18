using Dapplo.Windows.Native;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Dapplo.Windows.Test {
	[TestClass]
	public class TestGetDisplays {
		[TestMethod]
		public void TestMethod1() {
			foreach(var display in User32.GetDisplays()) {
				Debug.WriteLine("Device {0} - Bounds: {1} - Flags: {2}", display.DeviceName, display.Bounds.ToString(), display.Flags.ToString());
			}
		}
	}
}
