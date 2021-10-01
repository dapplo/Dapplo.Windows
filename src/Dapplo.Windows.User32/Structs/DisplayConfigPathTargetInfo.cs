using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigPathTargetInfo
    {
        public LUID adapterId;
        public uint id;
        public uint modeInfoIdx;
        DisplayConfigVideoOutputTechnology outputTechnology;
        DisplayConfigRotation rotation;
        DisplayConfigScaling scaling;
        DisplayConfigRational refreshRate;
        DisplayConfigScanlineOrdering scanLineOrdering;
        public bool targetAvailable;
        public uint statusFlags;
    }
}