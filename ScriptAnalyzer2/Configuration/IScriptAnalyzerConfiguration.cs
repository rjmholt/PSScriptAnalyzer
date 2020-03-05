using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration
{
    public interface IScriptAnalyzerConfiguration
    {
        IReadOnlyList<string> RulePaths { get; }

        IReadOnlyDictionary<string, IRuleConfiguration> Rules { get; }
    }

    internal class ScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        public ScriptAnalyzerConfiguration(
            IReadOnlyList<string> rulePaths,
            IReadOnlyDictionary<string, IRuleConfiguration> rules)
        {
            RulePaths = rulePaths;
            Rules = rules;
        }

        public IReadOnlyList<string> RulePaths { get; }

        public IReadOnlyDictionary<string, IRuleConfiguration> Rules { get; }
    }
}
