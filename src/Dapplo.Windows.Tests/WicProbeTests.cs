using Xunit;
namespace Dapplo.Windows.Tests;
public class WicProbeTests
{
    [Fact]
    public void Probe()
    {
        string report;
        try { report = Dapplo.Windows.Imaging.WicProbe.Run(); } catch (System.Exception ex) { report = "EXC " + ex; }
        Assert.Fail("PROBE-START\n" + report + "\nPROBE-END");
    }
}
