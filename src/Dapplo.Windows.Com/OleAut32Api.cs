#region Copyright (C) 2016-2019 Dapplo
//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2016-2019 Dapplo
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
#endregion

using System;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Common.Extensions;

namespace Dapplo.Windows.Com
{
    /// <summary>
    /// API for OLEAUT32
    /// </summary>
    public static class OleAut32Api
    {
        /// <summary>
        /// Get the active instance of the com object with the specified GUID
        /// </summary>
        /// <typeparam name="T">Type for the instance</typeparam>
        /// <param name="clsId">Guid</param>
        /// <returns>IDisposableCom of T</returns>
        public static IDisposableCom<T> GetActiveObject<T>(ref Guid clsId)
        {
            if (GetActiveObject(ref clsId, IntPtr.Zero, out object comObject).Succeeded())
            {
                return DisposableCom.Create((T)comObject);
            }

            return null;
        }

        /// <summary>
        /// Get the active instance of the com object with the specified progId
        /// </summary>
        /// <typeparam name="T">Type for the instance</typeparam>
        /// <param name="progId">string</param>
        /// <returns>IDisposableCom of T</returns>
        public static IDisposableCom<T> GetActiveObject<T>(string progId)
        {
            var clsId = Ole32Api.ClassIdFromProgId(progId);
            return GetActiveObject<T>(ref clsId);
        }

        #region Native methods

        /// <summary>
        /// For more details read <a href="https://docs.microsoft.com/en-gb/windows/desktop/api/oleauto/nf-oleauto-getactiveobject">this</a>
        /// </summary>
        /// <param name="rclsId">The class identifier (CLSID) of the active object from the OLE registration database.</param>
        /// <param name="pvReserved">Reserved for future use. Must be null.</param>
        /// <param name="ppunk">The requested active object.</param>
        /// <returns></returns>
        [DllImport("oleaut32.dll")]
        private static extern HResult GetActiveObject(ref Guid rclsId, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        #endregion
    }
}
