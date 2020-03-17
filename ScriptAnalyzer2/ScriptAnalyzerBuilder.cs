
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Json;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration.Psd;
using Microsoft.PowerShell.ScriptAnalyzer.Instantiation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer
{
    public class RuleProviderBuilder
    {
        public static RuleProviderBuilder FromConfigurationFile(string filePath)
        {
            string fileExt = Path.GetExtension(filePath).ToLower();

            switch (fileExt)
            {
                case ".json":
                    return new RuleProviderBuilder(new JsonFileConfigurationProvider(filePath).GetScriptAnalyzerConfiguration());

                default:
                    return new RuleProviderBuilder(new PsdFileConfigurationProvider(filePath).GetScriptAnalyzerConfiguration());
            }
        }

        public static RuleProviderBuilder FromConfigurationProvider(IConfigurationProvider configurationProvider)
        {
            return new RuleProviderBuilder(configurationProvider.GetScriptAnalyzerConfiguration());
        }

        private readonly IScriptAnalyzerConfiguration _configuration;

        private readonly List<IRuleProvider> _ruleProviders;

        public RuleProviderBuilder(IScriptAnalyzerConfiguration configuration)
        {
            _configuration = configuration;
            _ruleProviders = new List<IRuleProvider>();
        }

        public RuleProviderBuilder AddRuleProvider(IRuleProvider ruleProvider)
        {
            _ruleProviders.Add(ruleProvider);
            return this;
        }

        public RuleProviderBuilder AddRuleAssembly(Assembly assembly)
        {
            return AddRuleProvider(AssemblyRuleProvider.FromAssembly(assembly, _configuration));
        }

        public RuleProviderBuilder AddBuiltinRules()
        {
            return AddRuleAssembly(Assembly.GetExecutingAssembly());
        }

        public IRuleProvider Build()
        {
            if (_configuration.RulePaths != null)
            {
                foreach (string rulePath in _configuration.RulePaths)
                {
                    string ruleExt = Path.GetExtension(rulePath);

                    if (string.Equals(ruleExt, ".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        _ruleProviders.Add(AssemblyRuleProvider.FromAssembly(Assembly.LoadFile(rulePath), _configuration));
                    }
                }
            }

            return _ruleProviders.Count == 1
                ? _ruleProviders[0]
                : new CompositeRuleProvider(_ruleProviders);
        }
    }
}
