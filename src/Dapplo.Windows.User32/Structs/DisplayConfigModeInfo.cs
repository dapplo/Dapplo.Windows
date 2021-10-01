using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigModeInfo
    {
        public DisplayConfigModeInfoType infoType;
        public uint id;
        public LUID adapterId;
        public DisplayConfigModeInfoUnion modeInfo;
    }
}