
using Dapplo.Log.Facade;
using Dapplo.Log.XUnit;
using Dapplo.Windows.SafeHandles;
using Dapplo.Windows.Native;
using Xunit;
using Xunit.Abstractions;

namespace Dapplo.Windows.Tests
{
	public class SafeHandleTests
	{
		private static readonly LogSource Log = new LogSource();
		public SafeHandleTests(ITestOutputHelper testOutputHelper)
		{
			LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper); ;
		}

		[Fact]
		public void TestCreateCompatibleDC()
		{
			using (var desktopDcHandle = SafeWindowDcHandle.FromDesktop())
			{
				Assert.False(desktopDcHandle.IsInvalid);

				// create a device context we can copy to
				using (var safeCompatibleDcHandle = Gdi32.CreateCompatibleDC(desktopDcHandle))
				{
					Assert.False(safeCompatibleDcHandle.IsInvalid);
				}
			}
		}
	}
}
