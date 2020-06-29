using Microsoft.PowerShell.ScriptAnalyzer.Builder;
using Microsoft.PowerShell.ScriptAnalyzer.Configuration;
using Microsoft.PowerShell.ScriptAnalyzer.Rules;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.ScriptAnalyzer.Instantiation
{
    public class TypeRuleProviderFactory : IRuleProviderFactory
    {
        public static TypeRuleProviderFactory FromAssemblyFile(
            string assemblyPath)
        {
            return FromAssembly(Assembly.LoadFile(assemblyPath));
        }

        public static TypeRuleProviderFactory FromAssembly(
            Assembly ruleAssembly)
        {
            return new TypeRuleProviderFactory(ruleAssembly.GetExportedTypes());
        }

        private readonly IReadOnlyDictionary<string, IRuleConfiguration> _ruleConfigurationCollection;

        private readonly IReadOnlyList<Type> _types;

        public TypeRuleProviderFactory(
            IReadOnlyList<Type> types)
        {
            _types = types;
        }

        public IRuleProvider CreateRuleProvider(
            RuleComponentProvider ruleComponentProvider,
            IReadOnlyDictionary<string, IRuleConfiguration> ruleConfiguration)
        {
            return new TypeRuleProvider(GetRuleFactoriesFromTypes(ruleConfiguration, ruleComponentProvider, _types));
        }
        
        private static IReadOnlyDictionary<RuleInfo, TypeRuleFactory<ScriptRule>> GetRuleFactoriesFromTypes(
            IReadOnlyDictionary<string, IRuleConfiguration> ruleConfigurationCollection,
            RuleComponentProvider ruleComponentProvider,
            IReadOnlyList<Type> types)
        {
            var ruleFactories = new Dictionary<RuleInfo, TypeRuleFactory<ScriptRule>>();

            foreach (Type type in types)
            {
                if (RuleGeneration.TryGetRuleFromType(
                    ruleConfigurationCollection,
                    ruleComponentProvider,
                    type,
                    out RuleInfo ruleInfo,
                    out TypeRuleFactory<ScriptRule> factory))
                {
                    ruleFactories[ruleInfo] = factory;
                }
            }

            return ruleFactories;
        }
    }

}
