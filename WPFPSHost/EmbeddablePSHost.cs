//(c) Matthew Hobbs, 1/22/2008.  Licensed under Microsoft Public License (Ms-PL) (http://code.msdn.microsoft.com/PowerShellTunnel/Project/License.aspx)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Globalization;

namespace WPFPSHost
{
	/// <summary>
	/// A simple embeddable PSHost implementation that does not have any output.
	/// You might want to extend this to log error output.
	/// </summary>
	internal class EmbeddablePSHost : PSHost
	{
		#region private state
		private readonly CultureInfo currentCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
		private readonly CultureInfo currentUICulture = System.Threading.Thread.CurrentThread.CurrentUICulture;
		private readonly Guid instanceId = Guid.NewGuid();
        private readonly EmbeddablePSHostUserInterface embeddedPSHostUserInterface;
		private readonly Hashtable exposed = new Hashtable();
		private readonly PSObject privateData;
        IPSConsole Console;
		#endregion

		#region constructor
		public EmbeddablePSHost(IPSConsole pConsole)
		{
            Console = pConsole;
            embeddedPSHostUserInterface = new EmbeddablePSHostUserInterface(pConsole);
			this.privateData = new PSObject();
		}
		#endregion

		#region public overrides
		public override System.Globalization.CultureInfo CurrentCulture
		{
			get { return currentCulture; }
		}

		public override System.Globalization.CultureInfo CurrentUICulture
		{
			get { return currentUICulture; }
		}

		public override void EnterNestedPrompt()
		{
			throw new NotImplementedException("EnterNestedPrompt is not implemented.");
		}

		public override void ExitNestedPrompt()
		{
			throw new NotImplementedException("ExitNestedPrompt is not implemented.");
		}

		public override Guid InstanceId
		{
			get { return instanceId; }
		}

		public override string Name
		{
			get { return GetType().Name; }
		}

		public override void NotifyBeginApplication()
		{
		}

		public override void NotifyEndApplication()
		{
		}

		public override void SetShouldExit(int exitCode)
		{
            Console.ExitCode = exitCode;
		}

		public override PSHostUserInterface UI
		{
			get { return embeddedPSHostUserInterface; }
		}

		public override Version Version
		{
			get { return new Version(1, 0, 0, 0); }
		}

		public override PSObject PrivateData
		{
			get { return privateData; }
		}
		#endregion
	}
}