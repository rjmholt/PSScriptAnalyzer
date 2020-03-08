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
    public abstract class JsonConfigurationProvider : IConfigurationProvider
    {
        protected JsonConfigurationProvider()
        {
        }

        public IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration()
        {
            return GenerateScriptAnalyzerConfiguration();
        }

        protected abstract StreamReader GetJsonStream();

        private JsonScriptAnalyzerConfiguration GenerateScriptAnalyzerConfiguration()
        {
            var serializer = new JsonSerializer()
            {
                Converters = { new JsonConfigurationConverter() },
            };

            using (StreamReader streamReader = GetJsonStream())
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                return serializer.Deserialize<JsonScriptAnalyzerConfiguration>(jsonReader);
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
