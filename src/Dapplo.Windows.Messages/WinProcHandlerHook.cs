// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if !NETSTANDARD2_0
using System;
using System.Windows.Interop;

namespace Dapplo.Windows.Messages
{
    /// <summary>
    /// Wrapper of the HwndSourceHook for the WinProcHandler, to allow to specify a disposable
    /// </summary>
    public class WinProcHandlerHook
    {
        /// <summary>
        /// The actual HwndSourceHook
        /// </summary>
        public HwndSourceHook Hook { get; }

        /// <summary>
        /// Optional disposable which is called to make a cleanup possible
        /// </summary>
        public IDisposable Disposable { get; set; }

        /// <summary>
        /// Default constructor, taking the needed HwndSourceHook
        /// </summary>
        /// <param name="hook">HwndSourceHook</param>
        public WinProcHandlerHook(HwndSourceHook hook)
        {
            Hook = hook;
        }
    }
}
#endif