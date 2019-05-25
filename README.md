# Dapplo.Windows
This project contains code which adds Microsoft Windows specific functionality to your .NET Framework or dotnet core 3.x application

- Documentation can be found [here](http://www.dapplo.net/documentation/Dapplo.Windows)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/n99jafhbbp74n2w7?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-windows)
- Coverage: [![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)
- Dapplo.Windows: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.svg)](https://www.nuget.org/packages/Dapplo.Windows)
- Dapplo.Windows.Clipboard: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Clipboard.svg)](https://www.nuget.org/packages/Dapplo.Windows.Clipboard)
- Dapplo.Windows.Citrix: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Citrix.svg)](https://www.nuget.org/packages/Dapplo.Windows.Citrix)
- Dapplo.Windows.Com: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Com.svg)](https://www.nuget.org/packages/Dapplo.Windows.Com)
- Dapplo.Windows.Common: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Common.svg)](https://www.nuget.org/packages/Dapplo.Windows.Common)
- Dapplo.Windows.DesktopWindowsManager: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.DesktopWindowsManager.svg)](https://www.nuget.org/packages/Dapplo.Windows.DesktopWindowsManager)
- Dapplo.Windows.Dpi: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Dpi.svg)](https://www.nuget.org/packages/Dapplo.Windows.Dpi)
- Dapplo.Windows.Gdi32: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Gdi32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Gdi32)
- Dapplo.Windows.EmbeddedBrowser: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.EmbeddedBrowser.svg)](https://www.nuget.org/packages/Dapplo.Windows.EmbeddedBrowser)
- Dapplo.Windows.Input: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Input.svg)](https://www.nuget.org/packages/Dapplo.Windows.Input)
- Dapplo.Windows.Kernel32: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Kernel32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Kernel32)
- Dapplo.Windows.Messages: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Messages.svg)](https://www.nuget.org/packages/Dapplo.Windows.Messages)
- Dapplo.Windows.Multimedia: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Multimedia.svg)](https://www.nuget.org/packages/Dapplo.Windows.Multimedia)
- Dapplo.Windows.Shell32: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.Shell32.svg)](https://www.nuget.org/packages/Dapplo.Windows.Shell32)
- Dapplo.Windows.User32: [![NuGet package](https://img.shields.io/nuget/v/Dapplo.Windows.User32.svg)](https://www.nuget.org/packages/Dapplo.Windows.User32)

This is actually a lot of code I wrote for Greenshot, and with this I am trying to place it into an external repository so it's easier to maintain.

The following functionality is available:
* Hooking windows events (window moved, changed title etc), mouse & Keyboard
* Generating key presses or mouse input
* Clipboard access
* Getting the title, location etc of windows
* GDI / User 32 native method mapping
* Listing installed software
* A lot of the native structs and enums
... more


Here are some of the packages explained:

# Dapplo.Windows.Dpi
This helps to add DPI awareness to your application, and when using Windows.Forms be able to scale with DPI changes.
The easiest way is to have your form extend DpiAwareForm or, if you need default scaling use DpiUnwareForm.
When you want to handle DPI changes directly, e.g. scale bitmaps, is to use a DpiHandler.

# Dapplo.Windows.Messages
Has all the Windows Messages defined for some of the projects in the solution, so if someone uses part of the functionality not everything is included.

# Dapplo.Windows.Citrix
A library to add Citrix-Awareness to your application
```
if (WinFrame.IsAvailble) {
	// Citrix specific code here
	var clientHostname = WinFrame.QuerySessionInformation(InfoClasses.ClientName);
}
```

# Dapplo.Windows.EmbeddedBrowser
Drag ExtendedWebBrowser onto your form, and use it like the normal WebBrowser component.
To use the most recent version of the Internet Explorer, call:
```
InternetExplorerVersion.ChangeEmbeddedVersion();
```
an example is provided in the Dapplo.Windows.FormsExample project.

# Dapplo.Windows.Input
This library helps to process keyboard and mouse input, and can generate keyboard and mouse events.
It provides a low-level keyboard / mouse event hook, which is provided as an Observable (Reactive Extensions).
There are handlers to be able to react to a key combination, or a sequence of combinations.
Examples are provided in the unit-tests [here](https://github.com/dapplo/Dapplo.Windows/blob/master/src/Dapplo.Windows.Tests/KeyboardHookTests.cs)


# Dapplo.Windows.Clipboard
In Dapplo.Clipboard specialized code for using the Windows clipboard is place.
This is currently being developed and far from ready, it should provide a flexible and fluent API to use the Clipboard.
Currently there is already a simplified monitor, using System.Reactive, build in which allows you to subscribe to changes:
```
	var clipboardSubscription = ClipboardMonitor.OnUpdate.SubscribeOn( < ui SynchronizationContext> ).Where(args => args.Formats.Contains("MyFormat")).Subscribe(args =>
	{
		Debug.WriteLine("Detected my format between these possible formats: {0}", string.Join(",", args.Formats));
	});
```
This allows you to create simple code which calls you logic if the needed format is available, dispose the subscription when you are ready.

API:
ClipboardMonitor should publish a ClipboardContents object for every clipboard event.
The ClipboardContents object will get the formats and content at the first moment these are accessed.
Need a way to specify the OpenClipboard / CloseClipboard at the rights moments.

GetClipboardData should be used to get the contents, they should be converted to MemoryStreams?
The information is stored/maintained, until the ClipboardContents object is disposed?
This could be as soon as the next information is placed on the clipboard.
