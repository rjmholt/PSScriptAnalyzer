using Newtonsoft.Json.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using SMA = System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Execution
{
    public class PowerShellExecutor : IDisposable
    {
        public static PowerShellExecutor Create()
        {
            var pwsh = SMA.PowerShell.Create(RunspaceMode.NewRunspace);
            return new PowerShellExecutor(pwsh);
        }

        private readonly SMA.PowerShell _pwsh;

        private readonly object _lock;

        private PowerShellExecutor(SMA.PowerShell pwsh)
        {
            _pwsh = pwsh;
        }

        public Collection<T> InvokePowerShell<T>(PSCommand command)
        {
            lock (_lock)
            {
                try
                {
                    _pwsh.Commands = command;
                    return _pwsh.Invoke<T>();
                }
                finally
                {
                    _pwsh.Commands.Clear();
                }
            }
        }

        public Collection<PSObject> InvokePowerShell(PSCommand command)
        {
            return InvokePowerShell<PSObject>(command);
        }

        public void Dispose()
        {
            ((IDisposable)_pwsh).Dispose();
        }
    }
}
