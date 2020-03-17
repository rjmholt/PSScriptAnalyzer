using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public interface IScriptAnalyzerConfiguration
    {
        IReadOnlyList<string> RulePaths { get; }

        bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration);
    }
}
