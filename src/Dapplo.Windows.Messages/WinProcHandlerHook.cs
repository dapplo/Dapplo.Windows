//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

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