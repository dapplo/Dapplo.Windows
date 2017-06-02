# Dapplo.Windows
This project contains code which adds Microsoft Windows specific functionality to your application

THIS IS WORK IN PROGRESS!

- Documentation can be found [here](http://www.dapplo.net/blocks/Dapplo.Windows) (soon)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/n99jafhbbp74n2w7?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-windows)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.svg)](https://badge.fury.io/nu/Dapplo.Windows)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Clipboard.svg)](https://badge.fury.io/nu/Dapplo.Windows.Clipboard)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Citrix.svg)](https://badge.fury.io/nu/Dapplo.Windows.Citrix)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Com.svg)](https://badge.fury.io/nu/Dapplo.Windows.Com)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Common.svg)](https://badge.fury.io/nu/Dapplo.Windows.Common)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.DesktopWindowsManager)](https://badge.fury.io/nu/Dapplo.Windows.DesktopWindowsManager)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Dpi.svg)](https://badge.fury.io/nu/Dapplo.Windows.Dpi)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Gdi32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Gdi32)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Input.svg)](https://badge.fury.io/nu/Dapplo.Windows.Input)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Kernel32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Kernel32)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Messages.svg)](https://badge.fury.io/nu/Dapplo.Windows.Messages)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Multimedia.svg)](https://badge.fury.io/nu/Dapplo.Windows.Multimedia)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.Shell32.svg)](https://badge.fury.io/nu/Dapplo.Windows.Shell32)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.User32.svg)](https://badge.fury.io/nu/Dapplo.Windows.User32)

This is actually code I wrote for Greenshot, and with this I am trying to place it into an external repository so it's easier to maintain.

The following functionality is available:
* Hooking windows events (window moved, changed title etc), mouse & Keyboard
* Generating key presses or mouse input
* Getting the title, location etc of windows
* GDI / User 32 native method mapping
* A lot of the native structs and enums
... more

# Dapplo.Windows.Messages
Has all the Windows Messages defined for some of the projects in the solution, so if someone uses part of the functionality not everything is included.

# Dapplo.Windows.Citrix
A library to add Citrix-Awareness to your application
```
if (WinFrame.IsAvailble) {
	// Citrix specific code here
}
```

# Dapplo.Windows.Clipboard
In Dapplo.Clipboard specialized code for using the Windows clipboard is place.
This is currently being developed and far from ready, it should provide a flexible and fluent API to use the Clipboard.
Currently there is already a simplified monitor, using System.Reactive, build in which allows you to subscribe to changes:
```
	var clipboardSubscription = ClipboardMonitor.ClipboardUpdateEvents.Where(args => args.Formats.Contains("MyFormat")).Subscribe(args =>
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

# Dapplo.Windows.Dpi
A library to add Dpi-Awareness to your application
