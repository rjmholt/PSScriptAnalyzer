using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Execution;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class PSModuleRuleProviderFactory : IRuleProviderFactory
    {
        private readonly string _modulePath;
        
        public PSModuleRuleProviderFactory(string modulePath)
        {
            _modulePath = modulePath;
        }

        public IRuleProvider CreateRuleProvider(RuleComponentProvider ruleComponentProvider, IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurations)
        {
            if (!ruleComponentProvider.TryGetComponentInstance(out IPowerShellRuleExecutor psRuleExecutor))
            {
                return null;
            }

            var ipmoCommand = new PSCommand()
                .AddCommand("Import-Module", useLocalScope: true)
                .AddParameter("Name", _modulePath)
                .AddParameter("Force")
                .AddParameter("PassThru");

            PSModuleInfo ruleModule = psRuleExecutor
                .InvokeCommand<PSModuleInfo>(ipmoCommand)
                .First();

            var rules = new List<PSCommandRule>();

            foreach (FunctionInfo function in ruleModule.ExportedFunctions.Values)
            {
                if (TryCreateRuleFromFunction(psRuleExecutor, function, out PSCommandRule rule))
                {
                    rules.Add(rule);
                }
            }

            foreach (CmdletInfo cmdlet in ruleModule.ExportedCmdlets.Values)
            {
                if (TryCreateRuleFromCmdlet(psRuleExecutor, cmdlet, out PSCommandRule rule))
                {
                    rules.Add(rule);
                }
            }

            return new PSModuleRuleProvider(rules);
        }

        private bool TryCreateRuleFromFunction(
            IPowerShellRuleExecutor psRuleExecutor,
            FunctionInfo function,
            out PSCommandRule rule)
        {
            if (!RuleInfo.TryGetFromFunctionInfo(function, out RuleInfo ruleInfo))
            {
                rule = null;
                return false;
            }

            rule = new PSCommandRule(ruleInfo, psRuleExecutor, function);
            return true;
        }

        private bool TryCreateRuleFromCmdlet(
            IPowerShellRuleExecutor psRuleExecutor,
            CmdletInfo cmdlet,
            out PSCommandRule rule)
        {
            if (!RuleInfo.TryGetFromCmdletInfo(cmdlet, out RuleInfo ruleInfo))
            {
                rule = null;
                return false;
            }

            rule = new PSCommandRule(ruleInfo, psRuleExecutor, cmdlet);
            return true;
        }
    }
}
