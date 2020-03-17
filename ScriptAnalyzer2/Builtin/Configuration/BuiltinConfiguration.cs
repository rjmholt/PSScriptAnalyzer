using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.InMemory;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Builtin.Configuration
{
    public class BuiltinScriptAnalyzerConfiguration : DictionaryScriptAnalyzerConfiguration, IConfigurationProvider
    {
        public static BuiltinScriptAnalyzerConfiguration Instance => s_configurationLazy.Value;

        private static readonly RuleConfiguration s_commonConfiguration = new RuleConfiguration(new CommonConfiguration(true));

        private static readonly IReadOnlyDictionary<string, IRuleConfiguration> s_configurationDictionary = new Dictionary<string, IRuleConfiguration>
        {
            { "PS/AvoidUsingWMICmdlet", s_commonConfiguration }
        };

        private static readonly Lazy<BuiltinScriptAnalyzerConfiguration> s_configurationLazy =
            new Lazy<BuiltinScriptAnalyzerConfiguration>(CreateBuiltinConfiguration);

        public BuiltinScriptAnalyzerConfiguration(
            IReadOnlyList<string> rulePaths,
            IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurations)
                : base(rulePaths, ruleConfigurations)
        {
        }

        private static BuiltinScriptAnalyzerConfiguration CreateBuiltinConfiguration()
        {
            string builtinRulesAssemblyLocation = Assembly.GetExecutingAssembly().Location;

            return new BuiltinScriptAnalyzerConfiguration(
                new string[] { builtinRulesAssemblyLocation },
                s_configurationDictionary);
        }

        public IScriptAnalyzerConfiguration GetScriptAnalyzerConfiguration()
        {
            return this;
        }
    }
}
