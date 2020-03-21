using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json
{
    public class JsonScriptAnalyzerConfiguration : IScriptAnalyzerConfiguration
    {
        private static readonly JsonConfigurationConverter s_jsonConfigurationConverter = new JsonConfigurationConverter();

        public static JsonScriptAnalyzerConfiguration FromString(string jsonString)
        {
            return JsonConvert.DeserializeObject<JsonScriptAnalyzerConfiguration>(jsonString, s_jsonConfigurationConverter);
        }

        public static JsonScriptAnalyzerConfiguration FromFile(string filePath)
        {
            var serializer = new JsonSerializer()
            {
                Converters = { s_jsonConfigurationConverter },
            };

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return serializer.Deserialize<JsonScriptAnalyzerConfiguration>(jsonReader);
            }
        }

        private readonly JObject _ruleConfigurations;

        private readonly ConcurrentDictionary<string, IRuleConfiguration> _ruleConfigurationCache;

        public JsonScriptAnalyzerConfiguration(RuleExecutionMode ruleExecutionMode, IReadOnlyList<string> rulePaths, JObject ruleConfigurations)
        {
            _ruleConfigurations = ruleConfigurations;
            _ruleConfigurationCache = new ConcurrentDictionary<string, IRuleConfiguration>();
            RulePaths = rulePaths;
            RuleExecution = ruleExecutionMode;
        }

        public RuleExecutionMode RuleExecution { get; }

        public IReadOnlyList<string> RulePaths { get; }

        public bool TryGetRuleConfiguration(Type configurationType, string ruleName, out IRuleConfiguration configuration)
        {
            configuration = _ruleConfigurationCache.GetOrAdd(ruleName, (ruleName) => (IRuleConfiguration)_ruleConfigurations[ruleName].ToObject(configurationType));
            return true;
        }
    }
}
