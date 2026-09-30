// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dapplo.Windows.Desktop;
using Dapplo.Windows.DesktopWindowsManager;
using Dapplo.Windows.Icons.Enums;
using Dapplo.Windows.InstallerManager.Enums;
using Dapplo.Windows.Messages;
using Dapplo.Windows.Messages.Enums;
using Dapplo.Windows.Messages.Structs;
using Dapplo.Windows.Multimedia;
using Xunit;

namespace Dapplo.Windows.Tests;

/// <summary>
///     Tests for the feature pass "fd": the naming / API shape changes before 3.0.
///     None of these touch the desktop: no sound is played (only missing files / resources), no window is shown.
/// </summary>
public class FdFeatureTests
{
    /// <summary>
    ///     A registered message gets the same id when it's registered again, and its name can be resolved.
    /// </summary>
    [Fact]
    public void RegisteredWindowMessages_Register_RoundTripsTheName()
    {
        var name = "Dapplo.Windows.Tests.fd." + Guid.NewGuid().ToString("N");
        var id = RegisteredWindowMessages.Register(name);
        Assert.InRange(id, 0xC000u, 0xFFFFu);
        Assert.Equal(id, RegisteredWindowMessages.Register(name));
        Assert.Equal(name, RegisteredWindowMessages.GetName(id));
    }

    /// <summary>
    ///     A message below the registered range is named after the WindowsMessages value.
    /// </summary>
    [Fact]
    public void RegisteredWindowMessages_GetName_OfAPredefinedMessage()
    {
        Assert.Equal(nameof(WindowsMessages.WM_CLOSE), RegisteredWindowMessages.GetName((uint)WindowsMessages.WM_CLOSE));
    }

    /// <summary>
    ///     WindowMessage is a class in Dapplo.Windows.Messages, the .Structs namespace only has structs.
    /// </summary>
    [Fact]
    public void Messages_StructsNamespace_OnlyContainsStructs()
    {
        Assert.Equal("Dapplo.Windows.Messages", typeof(WindowMessage).Namespace);
        Assert.Equal("Dapplo.Windows.Messages.Enums", typeof(WindowsMessages).Namespace);
        var assembly = typeof(WindowMessage).Assembly;
        var nonStructs = assembly.GetExportedTypes()
            .Where(type => type.Namespace != null && type.Namespace.EndsWith(".Structs", StringComparison.Ordinal) && !type.IsValueType)
            .Select(type => type.FullName)
            .ToList();
        Assert.Empty(nonStructs);
        // The old helper and the Forms/WPF message info were replaced
        Assert.Null(assembly.GetType("Dapplo.Windows.Messages.WindowsMessage"));
        Assert.Null(assembly.GetType("Dapplo.Windows.Messages.WindowMessageInfo"));
    }

    /// <summary>
    ///     The native MSG structure exposes WParam / LParam with .NET casing.
    /// </summary>
    [Fact]
    public void Msg_HasPascalCaseParameters()
    {
        Assert.NotNull(typeof(Msg).GetProperty(nameof(Msg.WParam)));
        Assert.NotNull(typeof(Msg).GetProperty(nameof(Msg.LParam)));
        Assert.Null(typeof(Msg).GetProperty("wParam", BindingFlags.Public | BindingFlags.Instance));
    }

    /// <summary>
    ///     DrawIconExFlags lives in the namespace of the package which declares it.
    /// </summary>
    [Fact]
    public void DrawIconExFlags_IsInTheIconsNamespace()
    {
        Assert.Equal("Dapplo.Windows.Icons.Enums", typeof(DrawIconExFlags).Namespace);
        Assert.Equal(typeof(DrawIconExFlags).Assembly, typeof(Dapplo.Windows.Icons.CursorHelper).Assembly);
    }

    /// <summary>
    ///     RmShutdownType is a flag set with the native values (RmForceShutdown = 1, RmShutdownOnlyRegistered = 0x10).
    /// </summary>
    [Fact]
    public void RmShutdownType_HasTheNativeFlagValues()
    {
        Assert.NotNull(typeof(RmShutdownType).GetCustomAttribute<FlagsAttribute>());
        Assert.Equal(0u, (uint)RmShutdownType.Graceful);
        Assert.Equal(1u, (uint)RmShutdownType.Force);
        Assert.Equal(0x10u, (uint)RmShutdownType.OnlyRegistered);
        Assert.Equal(0x11u, (uint)(RmShutdownType.Force | RmShutdownType.OnlyRegistered));
    }

    /// <summary>
    ///     DwmApi has one colorization color property, it never throws.
    /// </summary>
    [Fact]
    public void DwmApi_ColorizationColor_IsTheOnlyColorProperty()
    {
        _ = DwmApi.ColorizationColor;
        var colorProperties = typeof(DwmApi).GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(property => property.PropertyType == typeof(System.Drawing.Color))
            .Select(property => property.Name)
            .ToList();
        Assert.Equal(new[] { nameof(DwmApi.ColorizationColor) }, colorProperties);
    }

    /// <summary>
    ///     A missing file doesn't play the default sound, PlayFile returns false.
    /// </summary>
    [Fact]
    public void WinMm_PlayFile_MissingFile_ReturnsFalse()
    {
        var missing = Path.Combine(Path.GetTempPath(), "fd-" + Guid.NewGuid().ToString("N") + ".wav");
        Assert.False(WinMm.PlayFile(missing));
        Assert.False(WinMm.PlayFile(string.Empty));
        Assert.Throws<ArgumentNullException>(() => WinMm.PlayFile(null));
    }

    /// <summary>
    ///     A missing resource doesn't play the default sound, PlayResource returns false.
    /// </summary>
    [Fact]
    public void WinMm_PlayResource_MissingResource_ReturnsFalse()
    {
        Assert.False(WinMm.PlayResource("FD_NO_SUCH_WAVE_RESOURCE"));
        Assert.Throws<ArgumentNullException>(() => WinMm.PlayResource(null));
    }

    /// <summary>
    ///     Every window of GetVisibleApplicationWindows passes IsVisibleApplicationWindow, and the desktop window is no application window.
    /// </summary>
    [Fact]
    public void GetVisibleApplicationWindows_MatchesIsVisibleApplicationWindow()
    {
        foreach (var window in InteropWindowQuery.GetVisibleApplicationWindows().Take(20))
        {
            if (!window.Exists())
            {
                // Destroyed after the snapshot
                continue;
            }
            Assert.NotEqual(0, window.GetCaption(forceUpdate: true).Length);
        }
        Assert.False(InteropWindowQuery.GetDesktopWindow().IsVisibleApplicationWindow());
    }

    /// <summary>
    ///     The public API names which were fixed: the old spelling doesn't exist anymore.
    /// </summary>
    [Fact]
    public void MisspelledNames_AreGone()
    {
        Assert.Null(typeof(Dapplo.Windows.Gdi32.GdiExtensions).GetMethod("AreRectangleCornersVisisble"));
        Assert.NotNull(typeof(Dapplo.Windows.Gdi32.GdiExtensions).GetMethod(nameof(Dapplo.Windows.Gdi32.GdiExtensions.AreRectangleCornersVisible)));
        Assert.Equal("CieXyzTriple", typeof(Dapplo.Windows.Gdi32.Structs.CieXyzTriple).Name);
        Assert.Null(typeof(InteropWindowQuery).GetMethod("IsTopLevel"));
        Assert.Null(typeof(InteropWindowQuery).GetMethod("GetTopLevelWindows"));
        Assert.Null(typeof(Dapplo.Windows.SystemState.SystemStateApi).GetMethod("CloseHandle"));
    }
}
