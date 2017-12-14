//(c) Matthew Hobbs, 1/22/2008.  Licensed under Microsoft Public License (Ms-PL) (http://code.msdn.microsoft.com/PowerShellTunnel/Project/License.aspx)
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;

namespace WPFPSHost
{
	/// <summary>
	/// An example PSHostUserInterface implementation with no output.
	/// 
	/// This is useful for a PowerShell Runspace hosted (embedded) within an
	/// application where you don't want a physical console for it.
	/// 
	/// In some applications you might want to log or otherwise handle some of
	/// these methods.
	/// </summary>
	internal class EmbeddablePSHostUserInterface : PSHostUserInterface
	{
        private IPSConsole myConsole = null;
        private EmbeddableRawUserInterface RawInferface;
        public EmbeddablePSHostUserInterface(IPSConsole pConsole)
        {
            RawInferface = new EmbeddableRawUserInterface(pConsole);
            myConsole = pConsole;
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            return myConsole.Prompt(caption, message, descriptions);
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            return myConsole.PromptForChoice(caption, message, choices, defaultChoice);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            return myConsole.PromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return myConsole.PromptForCredential(caption, message, userName, targetName);
        }

        ///// <summary>
        ///// Indicate to the host application that exit has
        ///// been requested. Pass the exit code that the host
        ///// application should use when exiting the process.
        ///// </summary>
        ///// <param name="exitCode"></param>
        //public void SetShouldExit(int exitCode)
        //{
        //    Write("We should EXIT now with exit code " + exitCode);
        //    //program.ShouldExit = true;
        //    //program.ExitCode = exitCode;
        //}
        public override string ReadLine()
        {
            return myConsole.ReadLine();
        }

        public override System.Security.SecureString ReadLineAsSecureString()
        {
            return myConsole.ReadLineAsSecureString();
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            myConsole.Write(foregroundColor, backgroundColor, value);
        }
        public override void Write(string value)
        {
            myConsole.Write(value);
        }
        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            myConsole.WriteLine(foregroundColor, backgroundColor, value);
        }
        public override void WriteLine()
        {
            myConsole.WriteLine(string.Empty);
        }
        public override void WriteLine(string value)
        {
            myConsole.WriteLine(value);
        }
        public override void WriteErrorLine(string value)
        {
            myConsole.WriteErrorLine(value);
        }
        public override void WriteDebugLine(string value)
        {
            myConsole.WriteDebugLine(value);
        }
        public override void WriteVerboseLine(string value)
        {
            myConsole.WriteVerboseLine(value);
        }
        public override void WriteWarningLine(string value)
        {
            myConsole.WriteWarningLine(value);
        }
        /// <summary>
        /// Progress is not implemented by this class. Since it's not
        /// required for the cmdlet to work, it is better to do nothing
        /// instead of throwing an exception.
        /// </summary>
        /// <param name="sourceId">See base class</param>
        /// <param name="record">See base class</param>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            myConsole.WriteProgress(sourceId, record);
           
        }
        public override PSHostRawUserInterface RawUI
        {
            get { return RawInferface; }
        }
    }
}