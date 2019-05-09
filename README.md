# Dapplo.Windows
This project contains code which adds Microsoft Windows specific functionality to your .NET Framework or dotnet core 3.x application

- Documentation can be found [here](http://www.dapplo.net/documentation/Dapplo.Windows)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/n99jafhbbp74n2w7?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-windows)
- Coverage: [![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Windows/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Windows?branch=master)
- NuGet package: [![NuGet package Dapplo.Windows](https://badge.fury.io/nu/Dapplo.Windows.svg)](https://badge.fury.io/nu/Dapplo.Windows)
- NuGet package: [![NuGet package Dapplo.Windows.Clipboard](https://badge.fury.io/nu/Dapplo.Windows.Clipboard.svg)](https://badge.fury.io/nu/Dapplo.Windows.Clipboard)
- NuGet package: [![NuGet package Dapplo.Windows.Citrix](https://badge.fury.io/nu/Dapplo.Windows.Citrix.svg)](https://badge.fury.io/nu/Dapplo.Windows.Citrix)
- NuGet package: [![NuGet package Dapplo.Windows.Com](https://badge.fury.io/nu/Dapplo.Windows.Com.svg)](https://badge.fury.io/nu/Dapplo.Windows.Com)
- NuGet package: [![NuGet package Dapplo.Windows.Common](https://badge.fury.io/nu/Dapplo.Windows.Common.svg)](https://badge.fury.io/nu/Dapplo.Windows.Common)
- NuGet package: [![NuGet package Dapplo.Windows.DesktopWindowsManager](https://badge.fury.io/nu/Dapplo.Windows.DesktopWindowsManager.svg)](https://badge.fury.io/nu/Dapplo.Windows.DesktopWindowsManager)
- NuGet package: [![NuGet package Dapplo.Windows.Dpi](https://badge.fury.io/nu/Dapplo.Windows.Dpi.svg)](https://badge.fury.io/nu/Dapplo.Windows.Dpi)
- NuGet package: [![NuGet package Dapplo.Windows.Gdi32](https://badge.fury.io/nu/Dapplo.Windows.Gdi32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Gdi32)
- NuGet package: [![NuGet package Dapplo.Windows.EmbeddedBrowser](https://badge.fury.io/nu/Dapplo.Windows.EmbeddedBrowser.svg)](https://badge.fury.io/nu/Dapplo.Windows.EmbeddedBrowser)
- NuGet package: [![NuGet package Dapplo.Windows.Input](https://badge.fury.io/nu/Dapplo.Windows.Input.svg)](https://badge.fury.io/nu/Dapplo.Windows.Input)
- NuGet package: [![NuGet package Dapplo.Windows.Kernel32](https://badge.fury.io/nu/Dapplo.Windows.Kernel32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Kernel32)
- NuGet package: [![NuGet package Dapplo.Windows.Messages](https://badge.fury.io/nu/Dapplo.Windows.Messages.svg)](https://badge.fury.io/nu/Dapplo.Windows.Messages)
- NuGet package: [![NuGet package Dapplo.Windows.Multimedia](https://badge.fury.io/nu/Dapplo.Windows.Multimedia.svg)](https://badge.fury.io/nu/Dapplo.Windows.Multimedia)
- NuGet package: [![NuGet package Dapplo.Windows.Shell32](https://badge.fury.io/nu/Dapplo.Windows.Shell32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Shell32)
- NuGet package: [![NuGet package Dapplo.Windows.User32](https://badge.fury.io/nu/Dapplo.Windows.User32.svg)](https://badge.fury.io/nu/Dapplo.Windows.User32)

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
