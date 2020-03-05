using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json
{
    internal class JsonRuleConfiguration : IRuleConfiguration
    {
        private readonly JObject _ruleJson;

        private readonly ConcurrentDictionary<Type, IRuleConfiguration> _configObjects;

        public JsonRuleConfiguration(JObject ruleJson)
        {
            _ruleJson = ruleJson;
            _configObjects = new ConcurrentDictionary<Type, IRuleConfiguration>();
        }

        public CommonConfiguration Common => throw new NotImplementedException();

        public TConfiguration AsConfigurationObject<TConfiguration>() where TConfiguration : IRuleConfiguration
        {
            return (TConfiguration)_configObjects.GetOrAdd(typeof(TConfiguration), (type) => _ruleJson.ToObject<TConfiguration>());
        }
    }

}
