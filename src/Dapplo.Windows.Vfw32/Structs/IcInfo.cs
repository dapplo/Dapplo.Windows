// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Vfw32.Structs
{
    /// <summary>
    /// Corresponds to the <c>ICINFO</c> structure, see <a href="http://msdn.microsoft.com/en-us/library/windows/desktop/dd743162%28v=vs.85%29.aspx">here</a>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public unsafe struct IcInfo
    {
        private const int VIDCF_QUALITY = 0x0001;
        private const int VIDCF_COMPRESSFRAMES = 0x0008;
        private const int VIDCF_FASTTEMPORALC = 0x0020;

        private int _dwSize;
        private uint _fccType;
        private uint _fccHandler;
        private uint _flags;
        private uint _dwVersion;
        private uint _dwVersionICM;
        private fixed char _szName[16];
        private fixed char _szDescription[128];
        private fixed char _szDriver[128];

        public bool SupportsQuality => (_flags & VIDCF_QUALITY) == VIDCF_QUALITY;

        public bool SupportsFastTemporalCompression => (_flags & VIDCF_FASTTEMPORALC) == VIDCF_FASTTEMPORALC;

        public bool RequestsCompressFrames => (_flags & VIDCF_COMPRESSFRAMES) == VIDCF_COMPRESSFRAMES;

        public uint FccHandler => _fccHandler;
        public uint FccType => _fccType;
        public int Size => _dwSize;

        public FourCC FourCC => new FourCC(_fccType);

        public string Name
        {
            get
            {
                fixed (char* name = _szName)
                {
                    return new string(name);
                }
            }
            set
            {
                fixed (char* name = _szName)
                {
                    var spaces = new string(' ', 16).ToCharArray();
                    Marshal.Copy(spaces, 0, new IntPtr(name), spaces.Length);
                    if (value != null)
                    {
                        char[] chars = value.ToCharArray();
                        Marshal.Copy(chars, 0, new IntPtr(name), chars.Length);
                    }
                }
            }
        }

        public string Description
        {
            get
            {
                fixed (char* desc = _szDescription)
                {
                    return new string(desc);
                }
            }
        }

        public IcInfo(FourCC type, string name = null)
        {
            _dwSize = SizeOf;
            _fccType = (uint)type;
            _fccHandler = 0;
            _flags = 0;
            _dwVersion = 0;
            _dwVersionICM = 0;
            Name = name;
        }

        public IcInfo(string type, string name = null)
        {
            _dwSize = SizeOf;
            _fccType = (uint)new FourCC(type);
            _fccHandler = 0;
            _flags = 0;
            _dwVersion = 0;
            _dwVersionICM = 0;
            Name = name;
        }

        public static int SizeOf => Marshal.SizeOf(typeof(IcInfo));
    }
}
