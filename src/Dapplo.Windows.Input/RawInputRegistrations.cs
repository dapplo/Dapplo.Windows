// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Structs;

namespace Dapplo.Windows.Input;

/// <summary>
/// The single owner of the raw input registrations of this process.
/// Windows keeps one registration per top level collection (usage page / usage) per process, and the last RegisterRawInputDevices call wins.
/// This keeps track of the flags every listener needs, registers the combined flags and removes the registration when nobody needs it anymore.
/// </summary>
internal static class RawInputRegistrations
{
    private static readonly object Lock = new();
    private static readonly Dictionary<(HidUsagePages UsagePage, ushort Usage), List<RawInputDeviceFlags>> Registrations = new();

    /// <summary>
    /// Add a registration for the specified devices, and register the combined flags with Windows
    /// </summary>
    /// <param name="hWnd">IntPtr with the window which receives the raw input</param>
    /// <param name="flags">RawInputDeviceFlags which this listener needs</param>
    /// <param name="devices">RawInputDevices</param>
    public static void Add(IntPtr hWnd, RawInputDeviceFlags flags, IEnumerable<RawInputDevices> devices)
    {
        lock (Lock)
        {
            var keys = ToKeys(hWnd, devices);
            foreach (var key in keys)
            {
                if (!Registrations.TryGetValue(key, out var flagsList))
                {
                    flagsList = new List<RawInputDeviceFlags>();
                    Registrations[key] = flagsList;
                }
                flagsList.Add(flags);
            }

            try
            {
                Apply(hWnd, keys);
            }
            catch
            {
                // Roll back, so the bookkeeping reflects what is registered
                RemoveFlags(flags, keys);
                throw;
            }
        }
    }

    /// <summary>
    /// Remove a registration for the specified devices, the combined flags of the remaining registrations are registered, or the device is removed when nobody needs it anymore
    /// </summary>
    /// <param name="hWnd">IntPtr with the window which receives the raw input</param>
    /// <param name="flags">RawInputDeviceFlags which were used with Add</param>
    /// <param name="devices">RawInputDevices which were used with Add</param>
    public static void Remove(IntPtr hWnd, RawInputDeviceFlags flags, IEnumerable<RawInputDevices> devices)
    {
        lock (Lock)
        {
            var keys = ToKeys(hWnd, devices);
            RemoveFlags(flags, keys);
            Apply(hWnd, keys);
        }
    }

    /// <summary>
    /// Combine the flags of multiple registrations for the same top level collection
    /// </summary>
    /// <param name="flags">IEnumerable of RawInputDeviceFlags</param>
    /// <returns>RawInputDeviceFlags</returns>
    internal static RawInputDeviceFlags Combine(IEnumerable<RawInputDeviceFlags> flags)
    {
        var combined = flags.Aggregate((RawInputDeviceFlags)0, (current, flag) => current | flag);
        return combined & ~RawInputDeviceFlags.Remove;
    }

    private static (HidUsagePages UsagePage, ushort Usage)[] ToKeys(IntPtr hWnd, IEnumerable<RawInputDevices> devices)
    {
        return devices
            .Select(device => RawInputApi.CreateRawInputDevice(hWnd, device))
            .Select(rawInputDevice => (rawInputDevice.UsagePage, rawInputDevice.Usage))
            .Distinct()
            .ToArray();
    }

    private static void RemoveFlags(RawInputDeviceFlags flags, IEnumerable<(HidUsagePages UsagePage, ushort Usage)> keys)
    {
        foreach (var key in keys)
        {
            if (!Registrations.TryGetValue(key, out var flagsList))
            {
                continue;
            }
            flagsList.Remove(flags);
            if (flagsList.Count == 0)
            {
                Registrations.Remove(key);
            }
        }
    }

    private static void Apply(IntPtr hWnd, IEnumerable<(HidUsagePages UsagePage, ushort Usage)> keys)
    {
        var rawInputDevices = keys.Select(key =>
        {
            if (Registrations.TryGetValue(key, out var flagsList) && flagsList.Count > 0)
            {
                return new RawInputDevice
                {
                    UsagePage = key.UsagePage,
                    Usage = key.Usage,
                    Flags = Combine(flagsList),
                    TargetHwnd = hWnd
                };
            }
            // RIDEV_REMOVE requires hwndTarget to be NULL
            return new RawInputDevice
            {
                UsagePage = key.UsagePage,
                Usage = key.Usage,
                Flags = RawInputDeviceFlags.Remove,
                TargetHwnd = IntPtr.Zero
            };
        }).ToArray();

        if (rawInputDevices.Length > 0)
        {
            RawInputApi.RegisterRawInput(rawInputDevices);
        }
    }
}
