using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public interface IRuleProviderFactory
    {
        IRuleProvider CreateRuleProvider(RuleComponentProvider ruleComponentProvider, IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurations);
    }
}
