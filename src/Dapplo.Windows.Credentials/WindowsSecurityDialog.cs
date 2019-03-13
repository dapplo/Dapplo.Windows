using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Dapplo.Windows.Credentials
{
    /// <summary>
    /// 
    /// </summary>
    public class WindowsSecurityDialog
    {
        private CredUiInfo _credui;
        private readonly StringBuilder _usernameBuffer = new StringBuilder();
        private readonly StringBuilder _passwordBuffer = new StringBuilder();
        private int _maxLength = 100;
        private bool _save;

        /// <summary>
        /// Default constructor specifying the 
        /// </summary>
        public WindowsSecurityDialog(string caption, string text)
        {
            _credui = new CredUiInfo(text, caption);
        }

        /// <summary>
        /// The username
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// The password
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Specifies if the dialog worked 
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Show the dialog
        /// </summary>
        public void Show()
        {
            // http://www.pinvoke.net/default.aspx/credui.CredUIPromptForWindowsCredentials
            const int CREDUIWIN_GENERIC = 0x1;
            uint authPackage = 0;
            // Show the dialog.
            IsSuccess = CredUIPromptForWindowsCredentials(ref _credui,
                            0,
                            ref authPackage,
                            IntPtr.Zero,
                            0,
                            out IntPtr outCredBuffer,
                            out uint outCredSize,
                            ref _save,
                            CREDUIWIN_GENERIC) == 0;
            var maxDomain = 100;
            if (!IsSuccess) {
                return;
            }
            // Try unpack credentials.
            var domainBuf = new StringBuilder(maxDomain);
            IsSuccess = CredUnPackAuthenticationBuffer(0, outCredBuffer, outCredSize, _usernameBuffer,
                ref _maxLength, domainBuf, ref maxDomain, _passwordBuffer, ref _maxLength);
            if (!IsSuccess)
            {
                return;
            }
            //clear the memory allocated by CredUIPromptForWindowsCredentials 
            CoTaskMemFree(outCredBuffer);
            var networkCredential = new NetworkCredential
            {
                UserName = _usernameBuffer.ToString(),
                Password = _passwordBuffer.ToString(),
                Domain = domainBuf.ToString()
            };
            Username = networkCredential.UserName;
            Password = networkCredential.Password;
        }

        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        private static extern uint CredUIPromptForWindowsCredentials(
            ref CredUiInfo notUsedHere,
            int authError,
            ref uint authPackage,
            IntPtr InAuthBuffer,
            uint InAuthBufferSize,
            out IntPtr refOutAuthBuffer,
            out uint refOutAuthBufferSize,
            ref bool fSave,
            int flags);

        [DllImport("credui.dll", CharSet = CharSet.Auto)]
        private static extern bool CredUnPackAuthenticationBuffer(int dwFlags, IntPtr pAuthBuffer, uint cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName, StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword, ref int pcchMaxPassword);

        [DllImport("ole32.dll")]
        private static extern void CoTaskMemFree(IntPtr ptr);
    }
}
