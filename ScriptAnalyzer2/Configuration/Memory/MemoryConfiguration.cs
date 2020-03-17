using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.InMemory
{
    public class LoadedConfigurationProvider : IConfigurationProvider
    {
        private readonly IScriptAnalyzerConfiguration _configuration;

        public LoadedConfigurationProvider(IScriptAnalyzerConfiguration scriptAnalyzerConfiguration)
        {
            _configuration = scriptAnalyzerConfiguration;
        }

        public IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration()
        {
            return _configuration;
        }
    }

    public class DictionaryScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        private readonly IReadOnlyDictionary<string, IRuleConfiguration> _ruleConfigurations;

        public DictionaryScriptAnalyzerConfiguration(
            IReadOnlyList<string> rulePaths,
            IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
            RulePaths = rulePaths;
        }

        public IReadOnlyList<string> RulePaths { get; }

        public bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration)
        {
            return _ruleConfigurations.TryGetValue(ruleName, out configuration);
        }
    }
}
