using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace WPFPSHost
{
    /// <summary>
    /// Interaction logic for PSConsole.xaml
    /// </summary>
    public partial class PSConsole : UserControl, IPSConsole,IDisposable
    {
        private Thread LoopIOThread;
        private EmbeddableRunspace Runspace { get; set; }
        private string _DirectRunScript = string.Empty;
        private bool _DirectRunScriptShowOutput = true;
        public bool IsStarted { get; set; }
        public PSConsole()
        {
            InitializeComponent();
            CommandManager.RegisterClassCommandBinding(typeof(PSConsole),
                                           new CommandBinding(ApplicationCommands.Stop, OnConsoleStop));
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
        }

        void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Stop();
        }

        public void StartPS()
        {
            StartRemotePS(null, null);
        }

        public void StartRemotePS(string pComputerName, PSCredential pCredential)
        {
            if (!IsStarted)
            {
                IsStarted = true;
                if (pComputerName == null)
                {
                    Runspace = new EmbeddableRunspace(this);
                }
                else
                {
                    Runspace = new EmbeddableRunspace(this, pComputerName, pCredential);
                }
                LoopIOThread = new Thread(LoopIORun);
                LoopIOThread.IsBackground = true;
                LoopIOThread.Start();
            }
        }

        public void Stop()
        {
            if (IsStarted)
            {
                IsStarted = false;
                if (LoopIOThread != null)
                {
                    LoopIOThread.Abort();
                    LoopIOThread = null;
                }
                Console.ReleaseInputLock();
                Console.Clear();
                OnConsoleStop(null, null);
                Runspace.Dispose();
                Runspace = null;
            }
        }

        public void RunScript(string script,bool pIsShowOutput)
        {
            if (Runspace == null)
            {
                throw new NullReferenceException("please start PSConsole");
            }

            if (IsScriptRunning)
            {
                throw new Exception("Other Command is running!");
            }
            _DirectRunScript = script;
            _DirectRunScriptShowOutput = pIsShowOutput;
            if (_DirectRunScriptShowOutput)
            {
                this.WriteLine(_DirectRunScript);
            }
            else
            {
                this.WriteLine(string.Empty);
            }
            Console.ReleaseInputLock();
        }

        private void OnConsoleStop(object sender, ExecutedRoutedEventArgs e)
        {
            if (Runspace != null)
            {
                try
                {
                    if (this.IsScriptRunning)
                    {
                        Runspace.CurrentPowerShell.Stop();
                    }
                }
                catch (Exception ex)
                {
                    this.WriteErrorLine(ex.ToString());
                }
            }
        }

        private void LoopIORun()
        {
            this.WriteLine("Windows PowerShell - Hosted on App\nCopyright (C) 2013 Microsoft Corporation. All rights reserved.");
            this.WriteLine(string.Empty);

            while (this.ExitCode == 0)
            {
                this.Write(Runspace.CommandPrompt);
                string ScriptCmd = Console.ReadLine();
                try
                {
                    if (string.IsNullOrEmpty(_DirectRunScript))
                    {
                        Runspace.RunScript(ScriptCmd, null);
                    }
                    else
                    {
                        if (_DirectRunScriptShowOutput)
                        {
                            Runspace.RunScript(_DirectRunScript, null);
                        }
                        else
                        {
                            Runspace.RunScript(_DirectRunScript, null, null);
                        }

                        _DirectRunScript = string.Empty;
                    }
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke((Action)delegate
                    {
                        MessageBox.Show(ex.Message);
                    });
                    break;
                }
            }
        }

        public Dictionary<string, System.Management.Automation.PSObject> Prompt(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.FieldDescription> descriptions)
        {
            Console.Write(
                  ConsoleColor.White,
                  null,
                  caption + "\n" + message + " ");
            Dictionary<string, PSObject> results =
                new Dictionary<string, PSObject>();
            foreach (FieldDescription fd in descriptions)
            {
                string[] label = GetHotkeyAndLabel(fd.Label);
                this.WriteLine(label[1]);
                
                 string userData ;
                 if (label[1] == "Credential")
                 {
                     userData = string.Empty;
                 }
                 else
                 {
                     userData = Console.ReadLine();
                 }
                 if (userData == null)
                 {
                     return null;
                 }
                results[fd.Name] = PSObject.AsPSObject(userData);
            }

            return results;
        }

        private static string[] GetHotkeyAndLabel(string input)
        {
            string[] result = new string[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                {
                    result[0] = fragments[1][0].ToString().
                    ToUpper(CultureInfo.CurrentCulture);
                }

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }

            return result;
        }

        public int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<System.Management.Automation.Host.ChoiceDescription> choices, int defaultChoice)
        {
            // Write the caption and message strings in Blue.
            Console.WriteLine(
                           ConsoleColor.White,
                           null,
                           caption + "\n" + message);

            // Convert the choice collection into something that's a
            // little easier to work with
            // See the BuildHotkeysAndPlainLabels method for details.
            Dictionary<string, PSObject> results =
                new Dictionary<string, PSObject>();
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display...
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format(
                    CultureInfo.CurrentCulture,
                    "|{0}> {1} ",
                    promptData[0, element],
                    promptData[1, element]));
            }

            sb.Append(String.Format(
                                    CultureInfo.CurrentCulture,
                                    "[Default is ({0}] ",
                                    promptData[0, defaultChoice]));

            // loop reading prompts until a match is made, the default is
            // chosen or the loop is interrupted with ctrl-C.
            while (true)
            {
                this.Write(ConsoleColor.Cyan, ConsoleColor.Black, sb.ToString());
                string data = Console.ReadLine().Trim().ToUpper(CultureInfo.CurrentCulture);

                // if the choice string was empty, use the default selection
                if (data.Length == 0)
                {
                    return defaultChoice;
                }

                // see if the selection matched and return the
                // corresponding index if it did...
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                    {
                        return i;
                    }
                }

                this.WriteErrorLine("Invalid choice: " + data);
            }
        }

        private static string[,] BuildHotkeysAndPlainLabels(Collection<ChoiceDescription> choices)
        {
            // we will allocate the result array
            string[,] hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }

            return hotkeysAndPlainLabels;
        }

        public System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName, System.Management.Automation.PSCredentialTypes allowedCredentialTypes, System.Management.Automation.PSCredentialUIOptions options)
        {
            if (!IsStarted)
            {
                return null;
            }
            return CredUtilities.CredUIPromptForCredential(caption, message, userName, targetName, allowedCredentialTypes, options);
        }

        public System.Management.Automation.PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            if (!IsStarted)
            {
                return null;
            }
            return CredUtilities.CredUIPromptForCredential(caption, message, userName, targetName, System.Management.Automation.PSCredentialTypes.Default| System.Management.Automation.PSCredentialTypes.Domain, System.Management.Automation.PSCredentialUIOptions.Default);
        }

        public System.Security.SecureString ReadLineAsSecureString()
        {
            return Console.ReadPassword();
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public void Write(string value)
        {
            Console.Write(null, null, value);
        }

        public void Write(string value, Block target)
        {
            Console.Write(null, null, value, target);
        }

        public void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Console.Write(foregroundColor, backgroundColor, value);
        }

        public void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target)
        {
            Console.Write(foregroundColor, backgroundColor, value, target);
        }

        public void WriteLine(string value)
        {
            Console.WriteLine(null, null, value);
        }

        public void WriteLine(string value, Block target)
        {
            Console.WriteLine(null, null, value, target);
        }

        public void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Console.WriteLine(foregroundColor, backgroundColor, value);
        }

        public void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value, Block target)
        {
            Console.WriteLine(foregroundColor, backgroundColor, value, target);
        }

        public void WriteDebugLine(string message)
        {
            Console.WriteLine(ConsoleColor.Blue, ConsoleColor.Black, message);
        }

        public void WriteDebugLine(string message, Block target)
        {
            Console.WriteLine(ConsoleColor.Blue, ConsoleColor.Black, message, target);
        }

        public void WriteErrorRecord(System.Management.Automation.ErrorRecord errorRecord)
        {
            Console.WriteLine(ConsoleColor.Red, ConsoleColor.Black, errorRecord.ToString());
        }

        public void WriteErrorLine(string value)
        {
            Console.WriteLine(ConsoleColor.Red, ConsoleColor.Black, value);
        }

        public void WriteVerboseLine(string message)
        {
            Console.WriteLine(ConsoleColor.White, ConsoleColor.Black, message);
        }

        public void WriteWarningLine(string message)
        {
            Console.WriteLine(ConsoleColor.Yellow, ConsoleColor.Black, message);
        }

        public void WriteNativeOutput(string message)
        {
            Console.WriteLine(ConsoleColor.White, ConsoleColor.Black, message);
        }

        public void WriteNativeError(string message)
        {
            Console.WriteLine(ConsoleColor.Red, ConsoleColor.Black, message);
        }

        public void WriteProgress(long sourceId, System.Management.Automation.ProgressRecord record)
        {

        }

        public int ExitCode { get; set; }

        public void SetBufferContents(System.Management.Automation.Host.Rectangle rectangle, BufferCell fill)
        {
            if (rectangle.Left == -1 && rectangle.Right == -1)
            {
                Console.Clear();
            }
            else
            {
                throw new NotImplementedException("The SetBufferContents method is not (yet) implemented!");
            }
        }

        public bool IsScriptRunning
        {
            get 
            {
                if (this.Runspace!=null && this.Runspace.CurrentPowerShell != null && this.Runspace.CurrentPowerShell.InvocationStateInfo.State == PSInvocationState.Running)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
