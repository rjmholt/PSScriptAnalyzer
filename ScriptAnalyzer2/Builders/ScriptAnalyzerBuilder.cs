using Microsoft.PowerShell.ScriptAnalyzer.Builder;
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
    public class ScriptAnalyzerBuilder : INeedsConfigurationStep<ScriptAnalyzerBuilder>
    {
        public static INeedsConfigurationStep<ScriptAnalyzerBuilder> Create()
        {
            return new ScriptAnalyzerBuilder();
        }

        private readonly List<Assembly> _ruleAssemblies;

        private IConfigurationProvider _configurationProvider;

        private ScriptAnalyzerBuilder()
        {
            _ruleAssemblies = new List<Assembly>();
        }

        ScriptAnalyzerBuilder INeedsConfigurationStep<ScriptAnalyzerBuilder>.WithPsdConfiguration(string input)
        {
            _configurationProvider = new PsdStringConfigurationProvider(input);
            return this;
        }

        ScriptAnalyzerBuilder INeedsConfigurationStep<ScriptAnalyzerBuilder>.WithJsonConfiguration(string input)
        {
            _configurationProvider = new JsonStringConfigurationProvider(input);
            return this;
        }

        ScriptAnalyzerBuilder INeedsConfigurationStep<ScriptAnalyzerBuilder>.WithConfigurationFile(string configurationPath)
        {
            string extension = Path.GetExtension(configurationPath);

            if (string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase))
            {
                _configurationProvider = new JsonFileConfigurationProvider(configurationPath);
            }
            else
            {
                // For now we try and parse any non-json-extension file as PSD
                _configurationProvider = new PsdFileConfigurationProvider(configurationPath);
            }

            return this;
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(string assemblyPath)
        {
            _ruleAssemblies.Add(Assembly.LoadFrom(assemblyPath));
            return this;
        }

        public ScriptAnalyzerBuilder AddRuleAssembly(Assembly assembly)
        {
            _ruleAssemblies.Add(assembly);
            return this;
        }

        public ScriptAnalyzer Build()
        {
            IScriptAnalyzerConfiguration configuration = _configurationProvider.GetScriptAnalyzerConfiguration();

            IRuleProvider ruleProvider = GetRuleProvider(configuration);

            return new ScriptAnalyzer(ruleProvider);
        }

        private IRuleProvider GetRuleProvider(IScriptAnalyzerConfiguration configuration)
        {
            if (_ruleAssemblies.Count == 1)
            {
                return AssemblyRuleProvider.FromAssembly(_ruleAssemblies[0], configuration);
            }

            var providers = new List<AssemblyRuleProvider>(_ruleAssemblies.Count);
            foreach (Assembly asm in _ruleAssemblies)
            {
                providers.Add(AssemblyRuleProvider.FromAssembly(asm, configuration));
            }

            return new CompositeRuleFactory(providers);
        }
    }
}
