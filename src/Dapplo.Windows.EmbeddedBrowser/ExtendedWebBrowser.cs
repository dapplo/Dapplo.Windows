// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
        }

    }
}
#endif