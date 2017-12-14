using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Security;
using System.Collections.ObjectModel;
using System.Management.Automation.Host;
using System.Windows.Documents;

namespace WPFPSHost
{
    internal interface IPSConsole
    {
        Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions);
        int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice);
        PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options);
        PSCredential PromptForCredential(string caption, string message, string userName, string targetName);
        SecureString ReadLineAsSecureString();
        string ReadLine();
        void Write(string value);
        void Write(string value, Block target);
        void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target);
        void WriteLine(string value);
        void WriteLine(string value, Block target);
        void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value);
        void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target);
        void WriteDebugLine(string message);
        void WriteDebugLine(string message, Block target);
        void WriteErrorRecord(ErrorRecord errorRecord);
        void WriteErrorLine(string value);
        void WriteVerboseLine(string message);
        void WriteWarningLine(string message);
        void WriteNativeOutput(string message);
        void WriteNativeError(string message);
        void WriteProgress(long sourceId, ProgressRecord record);
        int ExitCode { get; set; }
        void SetBufferContents(Rectangle rectangle, BufferCell fill);

    }
}
