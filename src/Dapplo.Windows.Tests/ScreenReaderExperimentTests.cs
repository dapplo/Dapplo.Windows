// TEMP experiment, not for master
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Xunit;

namespace Dapplo.Windows.Tests;

public class ScreenReaderExperimentTests
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    private static string WaitFor(bool expected, int seconds)
    {
        var start = Clock.ElapsedMilliseconds;
        while (Clock.ElapsedMilliseconds - start < seconds * 1000)
        {
            if (User32Api.IsScreenReaderRunning() == expected) return $"flag {expected} after {Clock.ElapsedMilliseconds - start} ms";
            Thread.Sleep(50);
        }
        return $"flag still {!expected} after {seconds} s";
    }

    private static void KillAll(string name)
    {
        foreach (var p in Process.GetProcessesByName(name)) { try { p.Kill(); p.WaitForExit(5000); } catch { } }
    }

    [Fact]
    public void ScreenReader_Messages()
    {
        var messages = new ConcurrentQueue<string>();
        using var subscription = EnvironmentMonitor.EnvironmentUpdateEvents
            .Subscribe(e => { if (e.SystemParametersInfoAction == SystemParametersInfoActions.SPI_SETSCREENREADER) messages.Enqueue($"{Clock.ElapsedMilliseconds}ms SETSCREENREADER area={e.Area ?? "null"} flag={User32Api.IsScreenReaderRunning()}"); });
        Thread.Sleep(500);
        var report = new StringBuilder($"initial flag {User32Api.IsScreenReaderRunning()}. ");
        string Drain() { var list = new System.Collections.Generic.List<string>(); while (messages.TryDequeue(out var m)) list.Add(m); return list.Count == 0 ? "NO MESSAGES" : string.Join("; ", list); }

        // Control: set it ourselves with SPIF_SENDCHANGE
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 1, (string)null, SystemParametersInfoBehaviors.SendChange);
        Thread.Sleep(1000);
        report.Append($"CONTROL set: {Drain()}. ");
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 0, (string)null, SystemParametersInfoBehaviors.SendChange);
        Thread.Sleep(1000);
        report.Append($"CONTROL reset: {Drain()}. ");
        // With UpdateIniFile | SendChange, as NVDA
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 1, (string)null, SystemParametersInfoBehaviors.UpdateIniFile | SystemParametersInfoBehaviors.SendChange);
        Thread.Sleep(1000);
        report.Append($"CONTROL set with UpdateIniFile|SendChange: {Drain()}. ");
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 0, (string)null, SystemParametersInfoBehaviors.UpdateIniFile | SystemParametersInfoBehaviors.SendChange);
        Thread.Sleep(1000);
        report.Append($"CONTROL reset with UpdateIniFile|SendChange: {Drain()}. ");
        // Without SendChange
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 1, (string)null, SystemParametersInfoBehaviors.None);
        Thread.Sleep(1000);
        report.Append($"CONTROL set without SendChange: {Drain()} flag={User32Api.IsScreenReaderRunning()}. ");
        User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 0, (string)null, SystemParametersInfoBehaviors.SendChange);
        Thread.Sleep(1000);
        Drain();

        // Narrator
        var narrator = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Narrator.exe");
        if (File.Exists(narrator))
        {
            Process.Start(new ProcessStartInfo(narrator) { UseShellExecute = true });
            report.Append($"NARRATOR start: {WaitFor(true, 30)}; ");
            Thread.Sleep(2000);
            report.Append($"msgs: {Drain()}. ");
            KillAll("Narrator");
            report.Append($"NARRATOR killed: {WaitFor(false, 15)}; ");
            Thread.Sleep(1000);
            report.Append($"msgs: {Drain()}. ");
            if (User32Api.IsScreenReaderRunning()) User32Api.SystemParametersInfo(SystemParametersInfoActions.SPI_SETSCREENREADER, 0, (string)null, SystemParametersInfoBehaviors.SendChange);
            Thread.Sleep(500); Drain();
        }
        else report.Append("NO NARRATOR. ");

        // NVDA
        var observed = new ConcurrentQueue<string>();
        using var observing = EnvironmentMonitor.ScreenReaderChanges.Subscribe(v => observed.Enqueue($"{Clock.ElapsedMilliseconds}ms {v}"));
        var nvda = new[] { @"C:\Program Files (x86)\NVDA\nvda.exe", @"C:\Program Files\NVDA\nvda.exe" }.FirstOrDefault(File.Exists);
        if (nvda != null)
        {
            Process.Start(new ProcessStartInfo(nvda, "--minimal") { UseShellExecute = true });
            report.Append($"NVDA start: {WaitFor(true, 60)}; ");
            Thread.Sleep(2000);
            report.Append($"msgs: {Drain()}. ");
            Process.Start(new ProcessStartInfo(nvda, "-q") { UseShellExecute = true });
            report.Append($"NVDA quit (-q): {WaitFor(false, 30)}; ");
            Thread.Sleep(1000);
            report.Append($"msgs: {Drain()}. ");
            KillAll("nvda");
        }
        else report.Append("NO NVDA. ");
        report.Append($"ScreenReaderChanges: {string.Join(", ", observed)}. ");
        Assert.Fail("EXPERIMENT " + report);
    }
}
