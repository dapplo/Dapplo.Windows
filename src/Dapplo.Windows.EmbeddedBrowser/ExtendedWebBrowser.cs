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
using System.Windows.Forms;
using Dapplo.Windows.Com;

namespace Dapplo.Windows.EmbeddedBrowser
{
    /// <summary>
    ///     Used to show an extended embedded web-browser
    ///     See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms683797.aspx">here</a>
    ///     for more information on this interface
    /// </summary>
    public class ExtendedWebBrowser : WebBrowser
    {
        /// <inheritdoc />
        protected override WebBrowserSiteBase CreateWebBrowserSiteBase()
        {
            return new ExtendedWebBrowserSite(this);
        }

        /// <summary>
        /// Internal class
        /// </summary>
        protected class ExtendedWebBrowserSite : WebBrowserSite, IOleCommandTarget
        {
            private const int OleCmdDidShowScriptError = 40;

            private const int Ok = 0;
            private const int OleCmmdErrENotsupported = -2147221248;

            private static readonly Guid CGID_DocHostCommandHandler = new Guid("F38BC242-B950-11D1-8918-00C04FC2C836");

            /// <summary>
            /// This creates an Extended WebBroser
            /// </summary>
            /// <param name="webBrowser">WebBrowser</param>
            public ExtendedWebBrowserSite(WebBrowser webBrowser) : base(webBrowser)
            {
            }

            #region IOleCommandTarget Members

            /// <inheritdoc />
            public int QueryStatus(Guid pguidCmdGroup, int cCmds, IntPtr prgCmds, IntPtr pCmdText)
            {
                return OleCmmdErrENotsupported;
            }

            /// <inheritdoc />
            public int Exec(Guid pguidCmdGroup, int nCmdId, int nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                if (pguidCmdGroup != CGID_DocHostCommandHandler)
                {
                    return OleCmmdErrENotsupported;
                }

                if (nCmdId == OleCmdDidShowScriptError)
                {
                    // do not need to alter pvaOut as the docs says, enough to return Ok here
                    return Ok;
                }

                return OleCmmdErrENotsupported;
            }

            #endregion
        }

    }
}
#endif