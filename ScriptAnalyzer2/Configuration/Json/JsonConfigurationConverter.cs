using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json
{
    internal class JsonConfigurationConverter : JsonConverter<JsonScriptAnalyzerConfiguration>
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override JsonScriptAnalyzerConfiguration ReadJson(
            JsonReader reader,
            Type objectType,
            [AllowNull] JsonScriptAnalyzerConfiguration existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JObject configObject = JObject.Load(reader);
            return new JsonScriptAnalyzerConfiguration(
                configObject["RulePaths"].ToObject<string[]>(),
                (JObject)configObject["Rules"]);
        }

        public override void WriteJson(JsonWriter writer, [AllowNull] JsonScriptAnalyzerConfiguration value, JsonSerializer serializer)
        {
            // Not needed - CanWrite is false
            throw new NotImplementedException();
        }
    }

}
