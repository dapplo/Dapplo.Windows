﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;

namespace Dapplo.Windows.App;

/// <summary>
///     A simple enum for the GetAppVisibilityOnMonitor method, this tells us if an App is visible on the supplied monitor.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
internal enum MonitorAppVisibility
{
    MAV_UNKNOWN = 0, // The mode for the monitor is unknown
    MAV_NO_APP_VISIBLE = 1,
    MAV_APP_VISIBLE = 2
}