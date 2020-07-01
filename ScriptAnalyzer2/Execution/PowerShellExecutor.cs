using Microsoft.PowerShell.ScriptAnalyzer.Utils;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using SMA = System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Execution
{
    internal interface IPowerShellRuleExecutor
    {
        Collection<T> InvokeCommand<T>(PSCommand command);
    }

    public class PowerShellExecutor : IPowerShellRuleExecutor
    {
        internal static PowerShellExecutor CreateForPSModuleRules()
        {
            var pwsh = SMA.PowerShell.Create();
            pwsh.RunspacePool = RunspaceFactory.CreateRunspacePool(minRunspaces: 1, maxRunspaces: 1);
            pwsh.RunspacePool.Open();
            pwsh.AddCommand("Import-Module").AddParameter("Name", PssaModule.RootPath).Invoke();
            return new PowerShellExecutor(pwsh);
        }

        private readonly SMA.PowerShell _pwsh;

        private PowerShellExecutor(SMA.PowerShell pwsh)
        {
            _pwsh = pwsh;
        }

        public Collection<T> InvokeCommand<T>(PSCommand command)
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
}
