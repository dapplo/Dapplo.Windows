using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Vfw32.Structs
{
    /// <summary>
    /// Corresponds to the <c>ICCOMPRESSFRAMES</c> structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    public struct CompressFramesInfo
    {
        private uint flags;
        public IntPtr OutBitmapInfoPtr;
        private int outputSize;
        public IntPtr InBitmapInfoPtr;
        private int inputSize;
        public int StartFrame;
        public int FrameCount;
        /// <summary>Quality from 0 to 10000.</summary>
        public int Quality;
        private int dataRate;
        /// <summary>Interval between key frames.</summary>
        /// <remarks>Equal to 1 if each frame is a key frame.</remarks>
        public int KeyRate;
        /// <summary></summary>
        public uint FrameRateNumerator;
        public uint FrameRateDenominator;
        private uint overheadPerFrame;
        private uint reserved2;
        private IntPtr getDataFuncPtr;
        private IntPtr setDataFuncPtr;
    }
}
