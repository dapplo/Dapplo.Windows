// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Dapplo.Windows.Input.Enums
{
    /// <summary>
    /// The commands to get the RawInputData
    /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms645568.aspx">RAWINPUTDEVICELIST structure</a>
    /// </summary>
    public enum RawInputDataCommands : uint
    {
        /// <summary>
        /// RID_INPUT: Get the raw data from the RAWINPUT structure.
        /// </summary>
        Input = 0x10000003,
        /// <summary>
        /// RID_HEADER: Get the header information from the RAWINPUT structure.
        /// </summary>
        Header = 0x10000005
    }
}
