// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Structs;
using Dapplo.Windows.User32;
using Dapplo.Windows.User32.Enums;
using Dapplo.Windows.Wpf;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the sweep (pass 5) fixes of Dapplo.Windows and Dapplo.Windows.User32
/// </summary>
public class S1SweepTests
{
    private sealed class DerivedInteropWindow : InteropWindow
    {
        public DerivedInteropWindow(IntPtr handle) : base(handle)
        {
        }
    }

    /// <summary>
    ///     A-42: Equals(object) is symmetric with Equals(IInteropWindow)
    /// </summary>
    [Fact]
    public void InteropWindow_Equals_IsSymmetric()
    {
        var window = new InteropWindow(new IntPtr(1234));
        var derived = new DerivedInteropWindow(new IntPtr(1234));
        Assert.True(window.Equals((object)derived));
        Assert.True(derived.Equals((object)window));
        Assert.Equal(window.GetHashCode(), derived.GetHashCode());
        Assert.False(window.Equals((object)new InteropWindow(new IntPtr(4321))));
    }

    /// <summary>
    ///     A-42: the cast of null results in IntPtr.Zero instead of a NullReferenceException
    /// </summary>
    [Fact]
    public void InteropWindow_ExplicitCast_NullIsZero()
    {
        InteropWindow window = null;
        Assert.Equal(IntPtr.Zero, (IntPtr)window);
        Assert.Equal(new IntPtr(42), ((InteropWindow)new IntPtr(42)).Handle);
    }

    /// <summary>
    ///     A-13: a window without children has no (bogus) children
    /// </summary>
    [Fact]
    public void GetTopWindows_LeafWindow_IsEmpty()
    {
        var messageWindow = InteropWindowFactory.CreateFor(SharedMessageWindow.Handle);
        Assert.Empty(InteropWindowQuery.GetTopWindows(messageWindow));
        Assert.False(messageWindow.HasChildren);
        Assert.Empty(messageWindow.GetChildren());
        Assert.Empty(messageWindow.GetDescendants());
    }

    /// <summary>
    ///     A-06: a (message-only) top-level window has no parent and no owner
    /// </summary>
    [Fact]
    public void GetParent_TopLevelWindow_HasNoParent()
    {
        var messageWindow = InteropWindowFactory.CreateFor(SharedMessageWindow.Handle);
        Assert.Equal(IntPtr.Zero, messageWindow.GetParent(true));
        Assert.False(messageWindow.HasParent);
        Assert.Null(messageWindow.GetParentWindow(true));
        Assert.Equal(IntPtr.Zero, messageWindow.GetOwner(true));
        Assert.False(messageWindow.HasOwner);
    }

    /// <summary>
    ///     A-11: getting the caption of a window of another thread of this process, while that thread waits for the caller, doesn't deadlock
    /// </summary>
    [Fact]
    public void GetCaption_OtherThreadOfProcess_DoesNotDeadlock()
    {
        var messageWindow = InteropWindowFactory.CreateFor(SharedMessageWindow.Handle);
        // Make sure the process and thread id are cached, so the check itself doesn't need the window thread
        messageWindow.GetProcessId();
        var completed = false;
        SharedMessageWindow.Invoke(_ =>
        {
            // The window thread is blocked here, while another thread reads the caption
            var captionTask = Task.Run(() => messageWindow.GetCaption(true));
            completed = captionTask.Wait(TimeSpan.FromSeconds(5));
        });
        Assert.True(completed);
        Assert.NotNull(messageWindow.Caption);
    }

    /// <summary>
    ///     A-11: getting the caption of a window of the calling thread works, instead of returning an empty string
    /// </summary>
    [Fact]
    public void GetCaption_SameThread_ReturnsCaption()
    {
        const string caption = "Dapplo caption test";
        string result = null;
        SharedMessageWindow.Invoke(hWnd =>
        {
            var previousCaption = User32Api.GetText(hWnd);
            User32Api.SetWindowText(hWnd, caption);
            try
            {
                result = InteropWindowFactory.CreateFor(hWnd).GetCaption(true);
            }
            finally
            {
                User32Api.SetWindowText(hWnd, previousCaption);
            }
        });
        Assert.Equal(caption, result);
    }

    /// <summary>
    ///     A-21: captions longer than 259 characters are not truncated
    /// </summary>
    [Fact]
    public void GetText_LongCaption_IsNotTruncated()
    {
        var caption = new string('x', 1000) + "end";
        string text = null;
        string internalText = null;
        SharedMessageWindow.Invoke(hWnd =>
        {
            var previousCaption = User32Api.GetText(hWnd);
            User32Api.SetWindowText(hWnd, caption);
            try
            {
                text = User32Api.GetText(hWnd);
                internalText = User32Api.GetInternalText(hWnd);
            }
            finally
            {
                User32Api.SetWindowText(hWnd, previousCaption);
            }
        });
        Assert.Equal(caption, text);
        Assert.Equal(caption, internalText);
    }

    /// <summary>
    ///     Captions around the size of the buffer on the stack (256 characters including the terminating 0) and of the doubled buffer
    /// </summary>
    [Fact]
    public void GetText_CaptionsAroundTheStackBuffer_AreComplete()
    {
        var lengths = new[] { 1, 253, 254, 255, 256, 257, 510, 511, 512, 513 };
        var results = new System.Collections.Generic.List<(string Caption, string Text, string InternalText)>();
        SharedMessageWindow.Invoke(hWnd =>
        {
            var previousCaption = User32Api.GetText(hWnd);
            try
            {
                foreach (var length in lengths)
                {
                    var caption = new string('x', length - 1) + "!";
                    User32Api.SetWindowText(hWnd, caption);
                    results.Add((caption, User32Api.GetText(hWnd), User32Api.GetInternalText(hWnd)));
                }
            }
            finally
            {
                User32Api.SetWindowText(hWnd, previousCaption);
            }
        });
        Assert.Equal(lengths.Length, results.Count);
        foreach (var (caption, text, internalText) in results)
        {
            Assert.Equal(caption, text);
            Assert.Equal(caption, internalText);
        }
    }

    /// <summary>
    ///     A-47: an exception in a predicate doesn't unwind through the native callback, but is rethrown after the enumeration
    /// </summary>
    [Fact]
    public void EnumerateWindows_ThrowingPredicate_IsRethrown()
    {
        Assert.Throws<InvalidOperationException>(() => WindowsEnumerator.EnumerateWindows(null, _ => throw new InvalidOperationException("test")));
        Assert.Throws<InvalidOperationException>(() => WindowsEnumerator.EnumerateWindowHandles(null, _ => throw new InvalidOperationException("test")));
    }

    /// <summary>
    ///     A-29, A-30, A-38: enum values
    /// </summary>
    [Fact]
    public void EnumValues_MatchWin32()
    {
        Assert.Equal(0x7FFFFFFFu, (uint)ObjectStates.STATE_SYSTEM_VALID);
        Assert.Equal(0x11, (int)WindowDisplayAffinity.ExcludeFromCapture);
        Assert.Equal(0x10000000u, (uint)DesktopAccessRight.GENERIC_ALL);
        Assert.Equal(0x1FFu, (uint)DesktopAccessRight.DESKTOP_ALL_SPECIFIC);
    }

    /// <summary>
    ///     A-46: WinEventInfo keeps the native 32-bit values
    /// </summary>
    [Fact]
    public void WinEventInfo_KeepsValues()
    {
        var info = WinEventInfo.Create(new IntPtr(1), Enums.WinEvents.EVENT_OBJECT_NAMECHANGE, new IntPtr(2), ObjectIdentifiers.Window, -1, 1234, uint.MaxValue);
        Assert.Equal(-1, info.IdChild);
        Assert.False(info.IsSelf);
        Assert.Equal(1234, info.EventThread);
        Assert.Equal(uint.MaxValue, info.EventTime);
    }

    /// <summary>
    ///     A-41: MapWindowPoints with a rectangle converts both corners
    /// </summary>
    [Fact]
    public void MapWindowPoints_Rect_MapsBothCorners()
    {
        // Screen to screen doesn't change anything, but the complete rectangle must survive the call
        var rect = new Common.Structs.NativeRect(10, 20, 30, 40);
        User32Api.MapWindowPoints(IntPtr.Zero, IntPtr.Zero, ref rect);
        Assert.Equal(new Common.Structs.NativeRect(10, 20, 30, 40), rect);
        var points = new[] { new Common.Structs.NativePoint(1, 2), new Common.Structs.NativePoint(3, 4) };
        User32Api.MapWindowPoints(IntPtr.Zero, IntPtr.Zero, points);
        Assert.Equal(new Common.Structs.NativePoint(3, 4), points[1]);
    }

    /// <summary>
    ///     A-43: the ignored classes can be changed
    /// </summary>
    [Fact]
    public void IgnoreClasses_AddRemove()
    {
        const string classname = "Dapplo.S1SweepTests.Class";
        Assert.True(InteropWindowQuery.AddIgnoreClass(classname));
        Assert.False(InteropWindowQuery.AddIgnoreClass(classname));
        Assert.Contains(classname, InteropWindowQuery.IgnoreClasses);
        Assert.True(InteropWindowQuery.RemoveIgnoreClass(classname));
        Assert.False(InteropWindowQuery.RemoveIgnoreClass(classname));
        Assert.DoesNotContain(classname, InteropWindowQuery.IgnoreClasses);
    }

    /// <summary>
    ///     A-32 / A-48: DisplayInfo sizes are the monitor sizes
    /// </summary>
    [Fact]
    public void DisplayInfo_SizeMatchesBounds()
    {
        foreach (var display in DisplayInfo.AllDisplayInfos)
        {
            Assert.Equal(display.Bounds.Width, display.ScreenWidth);
            Assert.Equal(display.Bounds.Height, display.ScreenHeight);
            Assert.True(display.DeviceName.Length <= 32);
        }
    }

    /// <summary>
    ///     A-28: ToBitmapSource keeps the alpha channel
    /// </summary>
    [StaFact]
    public void ToBitmapSource_KeepsAlpha()
    {
        using var bitmap = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
        bitmap.SetPixel(0, 0, Color.Transparent);
        bitmap.SetPixel(1, 0, Color.FromArgb(128, 255, 0, 0));
        var bitmapSource = bitmap.ToBitmapSource();
        Assert.Equal(2, bitmapSource.PixelWidth);
        var pixels = new byte[8];
        bitmapSource.CopyPixels(pixels, 8, 0);
        // BGRA
        Assert.Equal(0, pixels[3]);
        Assert.Equal(128, pixels[7]);
        Assert.Equal(255, pixels[6]);
    }

    /// <summary>
    ///     A-33: the installed software has no null entries
    /// </summary>
    [Fact]
    public void InstalledSoftware_HasNoNullEntries()
    {
        Assert.DoesNotContain(null, Software.InstallationInformation.InstalledSoftware().ToList());
    }
}
