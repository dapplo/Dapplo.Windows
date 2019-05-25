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
    /// This provides an API for OLE32
    /// </summary>
    public static class Ole32Api
    {
        /// <summary>
        /// This converts a ProgID (program ID) into a Guid with the clsId
        /// </summary>
        /// <param name="programId">string with the program ID</param>
        /// <returns>Guid with the clsId</returns>
        public static Guid ClassIdFromProgId(string programId)
        {
            if (CLSIDFromProgID(programId, out Guid clsId).Succeeded())
            {
                return clsId;
            }
            return clsId;
        }

        /// <summary>
        /// This converts a clsid (Class ID) into a ProgID (program ID)
        /// </summary>
        /// <param name="clsId">Guid with the clsid (Class ID)</param>
        /// <returns>string with the progid</returns>
        public static string ProgIdFromClassId(Guid clsId)
        {
            if (ProgIDFromCLSID(ref clsId, out string progId).Succeeded())
            {
                return progId;
            }

            return null;
        }

        #region Native methods

        /// <summary>
        /// See more <a href="https://docs.microsoft.com/en-us/windows/desktop/api/combaseapi/nf-combaseapi-clsidfromprogid">here</a>
        /// </summary>
        /// <param name="progId">string with the progId</param>
        /// <param name="clsId">Guid</param>
        /// <returns>HResult</returns>
        [DllImport("ole32.dll", ExactSpelling = true)]
        private static extern HResult CLSIDFromProgID([In] [MarshalAs(UnmanagedType.LPWStr)] string progId, [Out] out Guid clsId);

        /// <summary>
        /// See more <a href="https://docs.microsoft.com/en-us/windows/desktop/api/combaseapi/nf-combaseapi-progidfromclsid">here</a>
        /// </summary>
        /// <param name="clsId">Guid The CLSID for which the ProgID is to be requested.</param>
        /// <param name="lplpszProgId">string the ProgID string. The string that represents clsid includes enclosing braces.</param>
        /// <returns>HResult</returns>
        [DllImport("ole32.dll")]
        private static extern HResult ProgIDFromCLSID([In] ref Guid clsId, [MarshalAs(UnmanagedType.LPWStr)] out string lplpszProgId);
        #endregion
    }
}
