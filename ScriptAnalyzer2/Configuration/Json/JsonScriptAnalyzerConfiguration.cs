using Newtonsoft.Json.Linq;
using System;
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

        public bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration)
        {
            configuration = _ruleConfigurationCache.GetOrAdd(ruleName, (ruleName) => (IRuleConfiguration)_ruleConfigurations[ruleName].ToObject(configurationType));
            return true;
        }
    }
}
