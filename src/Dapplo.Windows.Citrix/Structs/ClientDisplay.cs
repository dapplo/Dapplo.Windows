// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;

namespace Dapplo.Windows.Citrix.Structs
{
    /// <summary>
    ///     This structure is returned when WFQuerySessionInformation is called with WFInfoClasses.ClientDisplay
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ClientDisplay
    {
        private readonly uint _horizontalResolution;
        private readonly uint _verticalResolution;
        private readonly uint _colorDepth;

        /// <summary>
        ///     Return the client's display size
        /// </summary>
        public NativeSize ClientSize => new NativeSize((int)_horizontalResolution, (int)_verticalResolution);

        /// <summary>
        ///     Returns the number of colors the client can display
        /// </summary>
        public uint ColorDepth
        {
            get
            {
                switch (_colorDepth)
                {
                    case 1:
                        return 4;
                    case 2:
                        return 8;
                    case 4:
                        return 16;
                    case 8:
                        return 24;
                    case 16:
                        return 32;
                }
                return _colorDepth;
            }
        }
    }
}