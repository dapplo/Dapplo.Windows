using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.User32.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DisplayConfigSourceMode
    {
        public uint width;
        public uint height;
        public DisplayConfigPixelformat pixelFormat;
        public NativePoint position;
    }
}