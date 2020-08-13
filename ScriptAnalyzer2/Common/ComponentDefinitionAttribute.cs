using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerShell.ScriptAnalyzer.Common
{
    public abstract class ComponentDefinitionAttribute : ScriptAnalyzerAttribute
    {
        private readonly Lazy<string> _descriptionLazy;

        public ComponentDefinitionAttribute(string name, string description)
        {
            Name = name;
            _descriptionLazy = new Lazy<string>(() => description);
        }


        public ComponentDefinitionAttribute(string name, Type descriptionResourceProvider, string descriptionResourceKey)
        {
            Name = name;
            _descriptionLazy = new Lazy<string>(() => GetStringFromResourceProvider(descriptionResourceProvider, descriptionResourceKey));
        }

        public string Name { get; }

        public string Namespace { get; set; }

        public string Description => _descriptionLazy.Value;

        private static string GetStringFromResourceProvider(Type resourceProvider, string resourceKey)
        {
            PropertyInfo resourceProperty = resourceProvider.GetProperty(resourceKey, BindingFlags.Static | BindingFlags.NonPublic);
            return (string)resourceProperty.GetValue(null);
        }
    }
}
