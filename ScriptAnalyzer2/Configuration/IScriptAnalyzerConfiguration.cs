using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public interface IScriptAnalyzerConfiguration
    {
        IReadOnlyList<string> RulePaths { get; }

        TRuleConfiguration GetRuleConfiguration<TRuleConfiguration>(string ruleName) where TRuleConfiguration : IRuleConfiguration;
    }
}
