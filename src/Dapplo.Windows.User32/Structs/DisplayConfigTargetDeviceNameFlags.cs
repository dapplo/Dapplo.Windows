using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs
{
    [Flags]
    public enum DisplayConfigTargetDeviceNameFlags : uint
    {
        friendlyNameFromEdid = 1,
        friendlyNameForced = 2,
        edidIdsValid = 4
    }
}