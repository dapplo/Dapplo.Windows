// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dapplo.Windows.Devices.Enums;

namespace Dapplo.Windows.Devices.Structs
{
    /// <summary>
    /// Contains information about a logical volume.
    /// See <a href="https://docs.microsoft.com/en-us/windows/win32/api/dbt/ns-dbt-dev_broadcast_volume">DEV_BROADCAST_VOLUME structure</a>
    ///
    /// Although the dbcv_unitmask member may specify more than one volume in any message, this does not guarantee that only one message is generated for a specified event.
    /// Multiple system features may independently generate messages for logical volumes at the same time.
    /// 
    /// Messages for media arrival and removal are sent only for media in devices that support a soft-eject mechanism.
    /// For example, applications will not see media-related volume messages for floppy disks.
    /// 
    /// Messages for network drive arrival and removal are not sent whenever network commands are issued, but rather when network connections will disappear as the result of a hardware event.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DevBroadcastVolume
    {
        private readonly int _size;
        // The device type, which determines the event-specific information that follows the first three members. 
        private readonly DeviceBroadcastDeviceType _deviceType;
        private readonly uint _reserved;
        // The logical unit mask identifying one or more logical units.
        // Each bit in the mask corresponds to one logical drive.
        // Bit 0 represents drive A, bit 1 represents drive B, and so on.
        private readonly uint _unitMask;
        // This parameter can be one of the following values
        // DBTF_MEDIA (1) - Change affects media in drive. If not set, change affects physical device or drive.
        // DBTF_NET (2) - Indicated logical volume is a network volume.
        private readonly ushort _flags;

        /// <summary>
        /// Returns a string with what drive letters are influenced in the message, this can be more than one!
        /// </summary>
        public string Drives
        {
            get
            {
                StringBuilder drives = new StringBuilder();
                foreach(var letter in Enumerable.Range(0,26))
                {
                    uint bit = 1u << letter;
                    if ((_unitMask & bit) != 0)
                    {
                        drives.Append((char)('A'+letter));
                    }
                }

                return drives.ToString();
            }
        }

        /// <summary>
        /// Change affects media in drive. 
        /// </summary>
        public bool IsMediaChange => (_flags & 0x0001) != 0;

        /// <summary>
        /// Indicated logical volume is a network volume.
        /// </summary>
        public bool IsNetworkVolume => (_flags & 0x0002) != 0;
    }
}
