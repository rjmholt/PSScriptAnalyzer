using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json
{
    public class JsonScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        private readonly JObject _ruleConfigurations;

        private readonly ConcurrentDictionary<string, IRuleConfiguration> _ruleConfigurationCache;

        public JsonScriptAnalyzerConfiguration(IReadOnlyList<string> rulePaths, JObject ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
            _ruleConfigurationCache = new ConcurrentDictionary<string, IRuleConfiguration>();
            RulePaths = rulePaths;
        }

        public IReadOnlyList<string> RulePaths { get; }

        public TRuleConfiguration GetRuleConfiguration<TRuleConfiguration>(string ruleName) where TRuleConfiguration : IRuleConfiguration
        {
            return (TRuleConfiguration)_ruleConfigurationCache.GetOrAdd(ruleName, (ruleName) => _ruleConfigurations[ruleName].ToObject<TRuleConfiguration>());
        }
    }
}
