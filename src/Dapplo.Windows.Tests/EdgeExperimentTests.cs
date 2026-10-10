// TEMP experiment, not for master
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Xunit;

namespace Dapplo.Windows.Tests;

public class EdgeExperimentTests
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Static;

    private static string EdgePath() => new[] { @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe", @"C:\Program Files\Microsoft\Edge\Application\msedge.exe" }.FirstOrDefault(File.Exists);

    private static string Html()
    {
        var html = Path.Combine(Path.GetTempPath(), "dapplo_scroll_test.html");
        var sb = new StringBuilder("<html><head><title>DapploScrollTest</title></head><body><h1>Test</h1>");
        for (var i = 0; i < 2000; i++) sb.Append("<p>Paragraph ").Append(i).Append(" lorem ipsum dolor sit amet</p>");
        File.WriteAllText(html, sb.Append("</body></html>").ToString());
        return html;
    }

    private static void KillEdge()
    {
        foreach (var p in Process.GetProcessesByName("msedge")) { try { p.Kill(); } catch { } }
        Thread.Sleep(1500);
    }

    private static (IntPtr TopLevel, IntPtr RenderWidget) Launch(string html)
    {
        KillEdge();
        var dir = Path.Combine(Path.GetTempPath(), "edgeexp" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo(EdgePath(),
            $"--user-data-dir=\"{dir}\" --no-first-run --no-default-browser-check --new-window --window-size=1000,800 \"{new Uri(html).AbsoluteUri}\"") { UseShellExecute = true });
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            var window = InteropWindowQuery.GetTopWindows().FirstOrDefault(w => (w.GetCaption() ?? "").Contains("DapploScrollTest"));
            if (window != null)
            {
                var render = window.GetChildren(true, true).FirstOrDefault(c => c.GetClassname() == "Chrome_RenderWidgetHostHWND");
                if (render != null) return (window.Handle, render.Handle);
            }
            Thread.Sleep(10);
        }
        return (IntPtr.Zero, IntPtr.Zero);
    }

    private static object Automation() => typeof(UiAutomationScroller).GetMethod("CreateAutomation", Flags, null, new[] { typeof(TimeSpan) }, null).Invoke(null, new object[] { TimeSpan.FromSeconds(2) });

    private static int Search(object automation, IntPtr handle) =>
        ((List<NativeRect>)typeof(UiAutomationScroller).GetMethod("FindScrollableAreasOnce", Flags).Invoke(null, new object[] { automation, handle, false, true })).Count;

    private static bool LooksIncomplete(object automation, IntPtr handle) =>
        (bool)typeof(UiAutomationAreas).GetMethod("LooksIncomplete", Flags).Invoke(null, new object[] { automation, handle, CancellationToken.None });

    private static (UiAutomationArea Root, bool ReadAgain) ReadOnce(object automation, IntPtr handle, int depth)
    {
        var bounds = InteropWindowFactory.CreateFor(handle).GetInfo().Bounds;
        var args = new object[] { automation, handle, depth, Math.Min(bounds.Width, bounds.Height) / 4, CancellationToken.None, false };
        var root = (UiAutomationArea)typeof(UiAutomationAreas).GetMethod("ReadOnce", Flags).Invoke(null, args);
        return (root, (bool)args[5]);
    }

    private static string Describe(UiAutomationArea root)
    {
        if (root == null) return "null";
        var sb = new StringBuilder($"root {root.ControlType} {root.Bounds.Width}x{root.Bounds.Height} c{root.Children.Count}");
        var pending = new Queue<(UiAutomationArea, int)>(root.Children.Select(c => (c, 1)));
        var count = 0;
        while (pending.Count > 0 && count < 14)
        {
            var (area, level) = pending.Dequeue();
            count++;
            sb.Append($" | L{level} {area.ControlType} {area.Bounds.Width}x{area.Bounds.Height} c{area.Children.Count}");
            foreach (var child in area.Children) pending.Enqueue((child, level + 1));
        }
        return sb.ToString();
    }

    private static string Probe(bool renderWidget)
    {
        var report = new StringBuilder(renderWidget ? "RENDER: " : "TOP: ");
        for (var round = 0; round < 2; round++)
        {
            var (top, render) = Launch(Html());
            if (top == IntPtr.Zero) return report.Append("no window").ToString();
            var watch = Stopwatch.StartNew();
            var areas = UiAutomationScroller.FindScrollableAreas(renderWidget ? render : top, false, null, true, null);
            report.Append($"R{round} cold: {areas.Count} areas in {watch.ElapsedMilliseconds} ms; ");
            watch.Restart();
            areas = UiAutomationScroller.FindScrollableAreas(renderWidget ? render : top, false, null, true, null);
            report.Append($"warm: {areas.Count} in {watch.ElapsedMilliseconds} ms. ");
        }
        return report.ToString();
    }

    [Fact]
    public void Edge_RenderWidget() { Assert.SkipWhen(EdgePath() is null, "no edge"); string r; try { r = Probe(true); } finally { KillEdge(); } Assert.Fail("EXPERIMENT " + r); }

    [Fact]
    public void Edge_TopLevel() { Assert.SkipWhen(EdgePath() is null, "no edge"); string r; try { r = Probe(false); } finally { KillEdge(); } Assert.Fail("EXPERIMENT " + r); }
}
