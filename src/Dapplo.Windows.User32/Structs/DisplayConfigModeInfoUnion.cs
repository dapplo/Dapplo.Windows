using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DisplayConfigModeInfoUnion
    {
        [FieldOffset(0)]
        public DisplayConfigTargetMode targetMode;
        [FieldOffset(0)]
        public DisplayConfigSourceMode sourceMode;
    }
}