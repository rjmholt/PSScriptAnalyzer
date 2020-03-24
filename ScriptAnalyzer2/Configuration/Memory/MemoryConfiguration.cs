using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.InMemory
{
    public class MemoryScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        public MemoryScriptAnalyzerConfiguration(
            RuleExecutionMode ruleExecutionMode,
            IReadOnlyList<string> rulePaths,
            MemoryRuleConfigurationCollection ruleConfigurationCollection)
        {
            RuleExecution = ruleExecutionMode;
            RulePaths = rulePaths;
            RuleConfiguration = ruleConfigurationCollection;
        }

        public RuleExecutionMode RuleExecution { get; }

        public IReadOnlyList<string> RulePaths { get; }

        public IRuleConfigurationCollection RuleConfiguration { get; }
    }

    public class MemoryRuleConfigurationCollection : IRuleConfigurationCollection
    {
        private readonly IReadOnlyDictionary<string, IRuleConfiguration> _ruleConfigurations;

        public MemoryRuleConfigurationCollection(IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
        }

        public bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration)
        {
            return _ruleConfigurations.TryGetValue(ruleName, out configuration);
        }
    }
}
