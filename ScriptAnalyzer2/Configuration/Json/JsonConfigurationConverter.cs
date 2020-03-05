using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json
{
    internal class JsonConfigurationConverter : JsonConverter<ScriptAnalyzerConfiguration>
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override ScriptAnalyzerConfiguration ReadJson(
            JsonReader reader,
            Type objectType,
            [AllowNull] ScriptAnalyzerConfiguration existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject configObject = JObject.Load(reader);

            var ruleConfigurations = new Dictionary<string, IRuleConfiguration>();
            foreach (KeyValuePair<string, JToken> ruleConfig in ((JObject)configObject["Rules"]))
            {
                ruleConfigurations[ruleConfig.Key] = new JsonRuleConfiguration((JObject)ruleConfig.Value);
            }

            return new ScriptAnalyzerConfiguration(
                configObject["RulePaths"].ToObject<string[]>(),
                ruleConfigurations);
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] ScriptAnalyzerConfiguration value, JsonSerializer serializer)
        {
            // Not needed - CanWrite is false
            throw new NotImplementedException();
        }
    }

}
