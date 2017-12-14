using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace WPFPSHost
{
    internal static class CredUtilities
    {
        [Flags]
        private enum CREDUI_FLAGS
        {
            ALWAYS_SHOW_UI = 0x80,
            COMPLETE_USERNAME = 0x800,
            DO_NOT_PERSIST = 2,
            EXCLUDE_CERTIFICATES = 8,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            INCORRECT_PASSWORD = 1,
            KEEP_USERNAME = 0x100000,
            PASSWORD_ONLY_OK = 0x200,
            PERSIST = 0x1000,
            REQUEST_ADMINISTRATOR = 4,
            REQUIRE_CERTIFICATE = 0x10,
            REQUIRE_SMARTCARD = 0x100,
            SERVER_CREDENTIAL = 0x4000,
            SHOW_SAVE_CHECK_BOX = 0x40,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            VALIDATE_USERNAME = 0x400
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszMessageText;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        private enum CredUIReturnCodes
        {
            ERROR_CANCELLED = 0x4c7,
            ERROR_INSUFFICIENT_BUFFER = 0x7a,
            ERROR_INVALID_ACCOUNT_NAME = 0x523,
            ERROR_INVALID_FLAGS = 0x3ec,
            ERROR_INVALID_PARAMETER = 0x57,
            ERROR_NO_SUCH_LOGON_SESSION = 0x520,
            ERROR_NOT_FOUND = 0x490,
            NO_ERROR = 0
        }

        [DllImport("credui", EntryPoint = "CredUIPromptForCredentialsW", CharSet = CharSet.Unicode)]
        private static extern CredUIReturnCodes CredUIPromptForCredentials(ref CREDUI_INFO pUiInfo, string pszTargetName, IntPtr Reserved, int dwAuthError, StringBuilder pszUserName, int ulUserNameMaxChars, StringBuilder pszPassword, int ulPasswordMaxChars, ref int pfSave, CREDUI_FLAGS dwFlags);

        internal static PSCredential CredUIPromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            if (string.IsNullOrEmpty(caption))
            {
                caption = "Enter Credential";
            }
            if (string.IsNullOrEmpty(message))
            {
                message = "Enter your Credential.";
            }
            if (caption.Length > 0x80)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid Caption", new object[] { 0x80 }));
            }
            if (message.Length > 0x400)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid Message", new object[] { 0x400 }));
            }
            if ((userName != null) && (userName.Length > 0x201))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid UserName", new object[] { 0x201 }));
            }

            CREDUI_INFO structure = new CREDUI_INFO
            {
                pszCaptionText = caption,
                pszMessageText = message
            };
            StringBuilder pszUserName = new StringBuilder(userName, 0x201);
            StringBuilder pszPassword = new StringBuilder(0x100);
            bool flag = false;
            int pfSave = Convert.ToInt32(flag);
            structure.cbSize = Marshal.SizeOf(structure);

            CREDUI_FLAGS dwFlags = CREDUI_FLAGS.DO_NOT_PERSIST;
            if ((allowedCredentialTypes & PSCredentialTypes.Domain) != PSCredentialTypes.Domain)
            {
                dwFlags |= CREDUI_FLAGS.GENERIC_CREDENTIALS;
                if ((options & PSCredentialUIOptions.AlwaysPrompt) == PSCredentialUIOptions.AlwaysPrompt)
                {
                    dwFlags |= CREDUI_FLAGS.ALWAYS_SHOW_UI;
                }
            }
            CredUIReturnCodes codes = CredUIReturnCodes.ERROR_INVALID_PARAMETER;
            if ((pszUserName.Length <= 0x201) && (pszPassword.Length <= 0x100))
            {
                codes = CredUIPromptForCredentials(ref structure, targetName, IntPtr.Zero, 0, pszUserName, 0x201, pszPassword, 0x100, ref pfSave, dwFlags);
            }
            if (codes == CredUIReturnCodes.NO_ERROR)
            {
                string str = null;
                if (pszUserName != null)
                {
                    str = pszUserName.ToString();
                }
                str = str.TrimStart(new char[] { '\\' });
                SecureString password = new SecureString();
                for (int i = 0; i < pszPassword.Length; i++)
                {
                    password.AppendChar(pszPassword[i]);
                    pszPassword[i] = '\0';
                }
                if (!string.IsNullOrEmpty(str))
                {
                    return new PSCredential(str, password);
                }
                return null;
            }
            return null;
        }
    }
}
