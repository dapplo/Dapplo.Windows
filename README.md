# Dapplo.Windows
A dapplo building block for querying and modifying native windows

THIS IS WORK IN PROGRESS!

- Documentation can be found [here](http://www.dapplo.net/blocks/Dapplo.Windows) (soon)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/n99jafhbbp74n2w7?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-windows)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Windows.svg)](https://badge.fury.io/nu/Dapplo.Windows)


This is actually code I wrote for Greenshot, and with this I am trying to place it into an external repository so it's easier to maintain.

The following functionality is available:
* Hooking windows events (window moved, changed title etc), mouse & Keyboard
* Generating key presses or mouse input
* Getting the title, location etc of windows
* GDI / User 32 native method mapping
* A lot of the native structs and enums
... more