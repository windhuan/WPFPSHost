//(c) Matthew Hobbs, 1/22/2008.  Licensed under Microsoft Public License (Ms-PL) (http://code.msdn.microsoft.com/PowerShellTunnel/Project/License.aspx)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;

namespace WPFPSHost
{
    internal class EmbeddableRunspace : IDisposable
    {
        #region private state
        private readonly EmbeddablePSHost embeddedPSHost;
        private readonly Runspace runspace;
        private readonly object instanceLock = new object();
        private PowerShell currentPowerShell;
        private WSManConnectionInfo RemoteConnectionInfo;
        #endregion

        public PowerShell CurrentPowerShell
        {
            get
            {
                return currentPowerShell;
            }
        }

        #region constructor
        public EmbeddableRunspace(IPSConsole pConsole)
        {
            embeddedPSHost = new EmbeddablePSHost(pConsole);
            this.runspace = RunspaceFactory.CreateRunspace(embeddedPSHost);
            this.runspace.Open();
        }
        public EmbeddableRunspace(IPSConsole pConsole, string pComputerName, PSCredential pCredential, AuthenticationMechanism pMechanism= AuthenticationMechanism.Credssp)
        {
            embeddedPSHost = new EmbeddablePSHost(pConsole);
            RemoteConnectionInfo = new WSManConnectionInfo(false, pComputerName, 5985, "/wsman", "http://schemas.microsoft.com/powershell/Microsoft.PowerShell", pCredential);
            //RemoteConnectionInfo.ComputerName = pComputerName;
            //RemoteConnectionInfo.Credential = pCredential;
            RemoteConnectionInfo.AuthenticationMechanism = pMechanism;
            //RemoteConnectionInfo.Port = 5985;
            this.runspace = RunspaceFactory.CreateRunspace(embeddedPSHost, RemoteConnectionInfo);
            this.runspace.Open();
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            runspace.Dispose();
        }
        #endregion

        #region private methods
   
        private void ReportException(Exception e)
        {
            if (e != null)
            {
                object error;
                IContainsErrorRecord icer = e as IContainsErrorRecord;
                if (icer != null)
                {
                    error = icer.ErrorRecord;
                }
                else
                {
                    error = (object)new ErrorRecord(e, "Host.ReportException", ErrorCategory.NotSpecified, null);
                }

                lock (this.instanceLock)
                {
                    this.currentPowerShell = PowerShell.Create();
                }

                this.currentPowerShell.Runspace = this.runspace;

                try
                {
                    this.currentPowerShell.AddScript("$input").AddCommand("out-string");

                    // Do not merge errors, this function will swallow errors.
                    Collection<PSObject> result;
                    PSDataCollection<object> inputCollection = new PSDataCollection<object>();
                    inputCollection.Add(error);
                    inputCollection.Complete();
                    result = this.currentPowerShell.Invoke(inputCollection);

                    if (result.Count > 0)
                    {
                        string str = result[0].BaseObject as string;
                        if (!string.IsNullOrEmpty(str))
                        {
                            // Remove \r\n, which is added by the Out-String cmdlet.
                            this.embeddedPSHost.UI.WriteErrorLine(str.Substring(0, str.Length - 2));
                        }
                    }
                }
                finally
                {
                    // Dispose of the pipeline and set it to null, locking it  because 
                    // currentPowerShell may be accessed by the ctrl-C handler.
                    lock (this.instanceLock)
                    {
                        this.currentPowerShell.Dispose();
                        this.currentPowerShell = null;
                    }
                }
            }
        }

        #endregion

        public Collection<PSObject> RunScript(string script, object input, string format = "Out-Default")
        {
            lock (this.instanceLock)
            {
                this.currentPowerShell = PowerShell.Create();
            }
            try
            {
                this.currentPowerShell.Runspace = this.runspace;
                this.currentPowerShell.AddScript(script);
                if (!string.IsNullOrEmpty(format))
                {
                    this.currentPowerShell.AddCommand(format);
                }
                this.currentPowerShell.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                
                if (input != null)
                {
                    return this.currentPowerShell.Invoke(new object[] { input });
                }
                else
                {
                    return this.currentPowerShell.Invoke();
                }
            }
            catch (RuntimeException rte)
            {
                this.ReportException(rte);
                return null;
            }
            finally
            {
                lock (this.instanceLock)
                {
                    if (currentPowerShell != null)
                    {
                        this.currentPowerShell.Dispose();
                        this.currentPowerShell = null;
                    }
                }
            }
        }

        public string CommandPrompt
        {
            get
            {
                var result = RunScript("get-location", null, null);
                if (result != null && result.Count > 0)
                {
                    if (RemoteConnectionInfo != null)
                    {
                        return string.Format("[{0}] PS {1}>", RemoteConnectionInfo.ComputerName, result[0].ToString());
                    }
                    return "PS " + result[0].ToString() + ">";
                }
                return "PS >";
            }
        }
    }
}
