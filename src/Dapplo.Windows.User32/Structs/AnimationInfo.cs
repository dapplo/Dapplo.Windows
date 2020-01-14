// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.User32.Structs
{
    /// <summary>
    ///     Describes the animation effects associated with user actions. This structure is used with the SystemParametersInfo
    ///     function when the SPI_GETANIMATION or SPI_SETANIMATION action value is specified.
    /// </summary>
    [SuppressMessage("Sonar Code Smell", "S1450:Private fields only used as local variables in methods should become local variables", Justification = "Interop!")]
    [StructLayout(LayoutKind.Sequential)]
    public struct AnimationInfo
    {
        private uint _cbSize;
        private int _iMinAnimate;

        /// <summary>
        ///     Factory method to create AnimationInfo
        /// </summary>
        /// <param name="enableAnimations"></param>
        /// <returns></returns>
        public static AnimationInfo Create(bool enableAnimations = true)
        {
            return new AnimationInfo
            {
                _cbSize = (uint) Marshal.SizeOf(typeof(AnimationInfo)),
                _iMinAnimate = enableAnimations ? 1 : 0
            };
        }
    }
}