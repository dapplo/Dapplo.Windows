using Microsoft.VisualStudio.TestTools.UnitTesting;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Native;

namespace Dapplo.Windows.Test
{
	[TestClass]
	public class SafeHandleTests
	{
		[TestMethod]
		public void TestCreateCompatibleDC()
		{
			using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
			{
				Assert.IsFalse(desktopDcHandle.IsInvalid);

				// create a device context we can copy to
				using (var safeCompatibleDcHandle = Gdi32.CreateCompatibleDC(desktopDcHandle))
				{
					Assert.IsFalse(safeCompatibleDcHandle.IsInvalid);
				}
			}
		}
	}
}
