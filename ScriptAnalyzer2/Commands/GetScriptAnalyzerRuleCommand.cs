using Microsoft.PowerShell.ScriptAnalyzer.Builtin.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Commands
{
    [Cmdlet(VerbsCommon.Get, "ScriptAnalyzerRule")]
    public class GetScriptAnalyzerRuleCommand : Cmdlet
    {
        [ValidateNotNullOrEmpty]
        [Parameter]
        public string ConfigurationPath { get; set; }

        protected override void EndProcessing()
        {
            var ruleProviderBuilder = ConfigurationPath != null
                ? RuleProviderBuilder.FromConfigurationFile(ConfigurationPath)
                : new RuleProviderBuilder(BuiltinScriptAnalyzerConfiguration.Instance);

            IRuleProvider ruleProvider = ruleProviderBuilder
                .AddBuiltinRules()
                .Build();

            foreach (RuleInfo ruleInfo in ruleProvider.GetRuleInfos())
            {
                WriteObject(ruleInfo);
            }
        }
    }
}
