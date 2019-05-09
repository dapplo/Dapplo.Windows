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
using System.Security.Principal;
using Dapplo.Windows.Advapi32.Enums;
using Dapplo.Windows.Advapi32.Structs;
using Dapplo.Windows.Kernel32;

namespace Dapplo.Windows.Advapi32
{
    /// <summary>
    /// Native methods to get registry or the login session information
    /// </summary>
    public static class Advapi32Api
    {
        private const uint SeGroupLogonId = 0xC0000000; // from winnt.h

        /// <summary>
        /// Get the current Session-ID SID
        /// </summary>
        /// <returns>string with SessionId as SID</returns>
        public static string CurrentSessionId
        {
            get
            {
                int tokenInfLength = 0;
                // first call gets lenght of TokenInformation
                GetTokenInformation(WindowsIdentity.GetCurrent().Token, TokenInformationClasses.TokenGroups, IntPtr.Zero, tokenInfLength, out tokenInfLength);
                var tokenInformation = Marshal.AllocHGlobal(tokenInfLength);
                try
                {
                    var result = GetTokenInformation(WindowsIdentity.GetCurrent().Token, TokenInformationClasses.TokenGroups, tokenInformation, tokenInfLength, out tokenInfLength);
                    if (!result)
                    {
                        return string.Empty;
                    }
                    string retVal = string.Empty;
                    var groups = (TokenGroups)Marshal.PtrToStructure(tokenInformation, typeof(TokenGroups));
                    int sidAndAttrSize = Marshal.SizeOf(new SidAndAttributes());
                    for (int i = 0; i < groups.GroupCount; i++)
                    {
                        var sidAndAttributes = (SidAndAttributes)Marshal.PtrToStructure(new IntPtr(tokenInformation.ToInt64() + i * sidAndAttrSize + IntPtr.Size), typeof(SidAndAttributes));
                        if ((sidAndAttributes.Attributes & SeGroupLogonId) != SeGroupLogonId)
                        {
                            continue;
                        }

                        ConvertSidToStringSid(sidAndAttributes.Sid, out var pstr);
                        try
                        {
                            retVal = Marshal.PtrToStringAuto(pstr);
                        }
                        finally
                        {
                            Kernel32Api.LocalFree(pstr);
                        }
                        break;
                    }
                    return retVal;
                }
                finally
                {
                    Marshal.FreeHGlobal(tokenInformation);
                }
            }
        }

        /// <summary>
        /// See more about <a href="https://docs.microsoft.com/en-us/windows/desktop/api/sddl/nf-sddl-convertsidtostringsida">ConvertSidToStringSidA function</a>
        /// The ConvertSidToStringSid function converts a security identifier (SID) to a string format suitable for display, storage, or transmission.
        /// </summary>
        /// <param name="pSid">IntPtr</param>
        /// <param name="ptrSid">IntPtr</param>
        /// <returns>bool</returns>
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool ConvertSidToStringSid(IntPtr pSid, out IntPtr ptrSid);

        /// <summary>
        /// The GetTokenInformation function retrieves a specified type of information about an access token. The calling process must have appropriate access rights to obtain the information.
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/aa446671(v=vs.85).aspx">GetTokenInformation function</a>
        /// </summary>
        /// <param name="tokenHandle">A handle to an access token from which information is retrieved. If TokenInformationClass specifies TokenSource, the handle must have TOKEN_QUERY_SOURCE access. For all other TokenInformationClass values, the handle must have TOKEN_QUERY access.</param>
        /// <param name="tokenInformationClasses">Specifies a value from the TOKEN_INFORMATION_CLASS enumerated type to identify the type of information the function retrieves. Any callers who check the TokenIsAppContainer and have it return 0 should also verify that the caller token is not an identify level impersonation token. If the current token is not an app container but is an identity level token, you should return AccessDenied.</param>
        /// <param name="tokenInformation">A pointer to a buffer the function fills with the requested information. The structure put into this buffer depends upon the type of information specified by the TokenInformationClass parameter.</param>
        /// <param name="tokenInformationLength">Specifies the size, in bytes, of the buffer pointed to by the TokenInformation parameter. If TokenInformation is NULL, this parameter must be zero.</param>
        /// <param name="returnLength">A pointer to a variable that receives the number of bytes needed for the buffer pointed to by the TokenInformation parameter. If this value is larger than the value specified in the TokenInformationLength parameter, the function fails and stores no data in the buffer.
        /// If the value of the TokenInformationClass parameter is TokenDefaultDacl and the token has no default DACL, the function sets the variable pointed to by ReturnLength to sizeof(TOKEN_DEFAULT_DACL) and sets the DefaultDacl member of the TOKEN_DEFAULT_DACL structure to NULL.</param>
        /// <returns>If the function succeeds, the return value is nonzero.</returns>
        [DllImport("advapi32", SetLastError = true)]
        public static extern bool GetTokenInformation(
            IntPtr tokenHandle,
            TokenInformationClasses tokenInformationClasses,
            IntPtr tokenInformation,
            int tokenInformationLength,
            out int returnLength);

        /// <summary>
        /// Notifies the caller about changes to the attributes or contents of a specified registry key.
        /// </summary>
        /// <param name="hKey">IntPtr A handle to an open registry key.</param>
        /// <param name="watchSubtree">If this parameter is TRUE, the function reports changes in the specified key and its subkeys. If the parameter is FALSE, the function reports changes only in the specified key.</param>
        /// <param name="notifyFilter">RegChangeNotifyFilter A value that indicates the changes that should be reported. </param>
        /// <param name="hEvent"></param>
        /// <param name="asynchronous">If this parameter is TRUE, the function returns immediately and reports changes by signaling the specified event. If this parameter is FALSE, the function does not return until a change has occurred.</param>
        /// <returns></returns>
        [DllImport("advapi32", SetLastError = true)]
        public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool watchSubtree, RegistryNotifyFilter notifyFilter, IntPtr hEvent, bool asynchronous);

        /// <summary>
        /// Opens the specified registry key. Note that key names are not case sensitive.
        /// To perform transacted registry operations on a key, call the RegOpenKeyTransacted function.
        /// </summary>
        /// <param name="hKey">IntPtr A handle to an open registry key.</param>
        /// <param name="subKey">
        /// The name of the registry subkey to be opened.
        /// Key names are not case sensitive.
        /// The lpSubKey parameter can be a pointer to an empty string. If lpSubKey is a pointer to an empty string and hKey is HKEY_CLASSES_ROOT, phkResult receives the same hKey handle passed into the function. Otherwise, phkResult receives a new handle to the key specified by hKey.
        /// The lpSubKey parameter can be NULL only if hKey is one of the predefined keys. If lpSubKey is NULL and hKey is HKEY_CLASSES_ROOT, phkResult receives a new handle to the key specified by hKey. Otherwise, phkResult receives the same hKey handle passed in to the function.
        /// </param>
        /// <param name="ulOptions">RegistryOpenOptions</param>
        /// <param name="samDesired">RegistryKeySecurityAccessRights</param>
        /// <param name="hOpenedKey">UIntPtr a handle to the registry key</param>
        /// <returns></returns>
        [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int RegOpenKeyEx(IntPtr hKey, string subKey, RegistryOpenOptions ulOptions, RegistryKeySecurityAccessRights samDesired, out IntPtr hOpenedKey);

        /// <summary>
        /// Closes a handle to the specified registry key.
        /// </summary>
        /// <param name="hKey">UIntPtr a handle to the registry key</param>
        /// <returns></returns>
        [DllImport("advapi32", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);
    }
}
