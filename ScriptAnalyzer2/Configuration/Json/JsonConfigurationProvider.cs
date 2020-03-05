using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

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

    public abstract class JsonConfigurationProvider : IConfigurationProvider
    {
        private readonly Lazy<ScriptAnalyzerConfiguration> _configurationLazy;

        public JsonConfigurationProvider()
        {
            _configurationLazy = new Lazy<ScriptAnalyzerConfiguration>(GenerateScriptAnalyzerConfiguration);
        }

        public IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration()
        {
            return _configurationLazy.Value;
        }

        protected abstract StreamReader GetJsonStream();

        private ScriptAnalyzerConfiguration GenerateScriptAnalyzerConfiguration()
        {
            var serializer = new JsonSerializer()
            {
                Converters = { new JsonConfigurationConverter() },
            };

            using (StreamReader streamReader = GetJsonStream())
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return serializer.Deserialize<ScriptAnalyzerConfiguration>(jsonReader);
            }
        }
    }

    public class JsonFileConfigurationProvider : JsonConfigurationProvider
    {
        private readonly string _jsonFilePath;

        private readonly Encoding _fileEncoding;

        public JsonFileConfigurationProvider(string jsonFilePath)
            : this(jsonFilePath, fileEncoding: null)
        {
        }

        public JsonFileConfigurationProvider(
            string jsonFilePath,
            Encoding fileEncoding)
        {
            _jsonFilePath = jsonFilePath;
            _fileEncoding = fileEncoding;
        }

        protected override StreamReader GetJsonStream()
        {
            return _fileEncoding != null
                ? new StreamReader(_jsonFilePath, _fileEncoding)
                : new StreamReader(_jsonFilePath, detectEncodingFromByteOrderMarks: true);
        }
    }

    public class JsonStringConfigurationProvider : JsonConfigurationProvider
    {
        private readonly string _jsonString;

        public JsonStringConfigurationProvider(string jsonString)
        {
            _jsonString = jsonString;
        }

        protected override StreamReader GetJsonStream()
        {
            return new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(_jsonString)));
        }
    }
}
